using System;
using System.Net.Sockets;

namespace FreeNet
{
    public class CNetworkService
    {
        // 클라이언트의 접속을 받아들이기 위한 객체
        CListener client_listener;

        // 메시지 수신, 전송시 필요한 오브젝트입니다.
        SocketAsyncEventArgsPool receive_event_args_pool;
        SocketAsyncEventArgsPool send_event_args_pool;


        // 메시지 수신, 전송시 .Net 비동기 소켓에서 사용할 버퍼를 관리할 객체입니다.

        BufferManager buffer_manager;

        // 클라이언트의 접속이 이루어졌을 때 호출되는 델리게이트 입니다.
        public delegate void SessionHandler(CUserToken token);
        public SessionHandler session_created_callback { get; set; }

        private int max_connections;
        private int buffer_size;
        private int pre_alloc_count;

        public void Initialize()
        {
           

            this.max_connections = 100;
            this.buffer_size = 4086;
            this.pre_alloc_count = 2;
            this.buffer_manager = new BufferManager(this.max_connections * this.buffer_size * this.pre_alloc_count, this.buffer_size)
            this.receive_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);
            this.send_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);

            SocketAsyncEventArgs arg = null;
            for (int i = 0; i < this.max_connections; i++)
            {
                CUserToken token = new CUserToken();

                // receive_pool
                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
                    arg.UserToken = token;


                    this.buffer_manager.SetBuffer(arg);

                    this.receive_event_args_pool.Push(arg);
                }

                // send Pool
                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_completed);
                    arg.UserToken = token;

                    this.buffer_manager.SetBuffer(arg);
                    this.send_event_args_pool.Push(arg);
                }
            }
        }
        public void listen(string host, int port, int backlog)
        {
            CListener listener = new CListener();
            listener.callback_on_newclient += on_new_client();
            listener.start(host, port, backlog);
        }

        void on_new_client(Socket client_socket, object token)
        {
            // 플레서 하나 꺼내와 사용합니다.
            SocketAsyncEventArgs receive_args = this.receive_event_args_pool.Pop();
            SocketAsyncEventArgs send_args = this.send_event_args_pool.Pop();

            // SocketAsyncEventArgs를 생성할 떄 만들어 두었던 CUserToken을 꺼내와서
            // 콜백 매소드의 파라미터로 넘겨줍니다.

            if (this.session_created_callback != null)
            {
                CUserToken user_token = receive_args.UserToken as CUserToken();
                this.session_created_callback(user_token);
            }

            // 이제 클라이언트로부터 데이터를 수신할 준비를 합니다.
            begin_receive(client_socket, receive_args, send_args);

        }

        void begin_receive(Socket socket , SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
        {
            // receive_args , send_args 아무곳에서나 꺼내와도 된다. 둘다 동일한 CUserToken을 물고 있다.
            CUserToken token = receive_args.UserToken as CUserToken;
            token.set_event_args(receive_args);

            // 생성된 클라이언트 소켓을 보관해 놓고 통신할 때 사용합니다.
            token.socket = socket;

            // 데이터를 받을 수 있도록 소켓 매소드를 호출해준다.
            // 비동기로 수신할 경우 워커 스레드에서 대기중으로 있다가 Completed에 설정해놓은 매소드가 호출 됩니다.
            // 동기로 완료될 경우에는 직접 완료 매소드를 호출해줘야 합니다.

            bool pending = socket.ReceiveAsync(receive_args);
            if (!pending)
            {
                process_receive(receive_args);
            }

                
        }

        void receive_completed(object sender ,SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {

                process_receive(e);
                return;
            }
            throw new ArgumentException("The last operation completed on the socket was not a receive.");
        }
        
        private void process_receive(SocketAsyncEventArgs e)
        {
            CUserToken token = e.UserToken as CUserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                // 이후의 작업은 CUserToken에 맡긴다.
                token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);

                // 다음 메시지 수신을 위해서 다시 ReceiveAsync매소드를 호출합니다.
                bool pending = token.socket.ReceiveAsync(e);
                if (!pending)
                {
                    process_receive(e);
                }
            }
            else
            {
                Console.WriteLine(string.Format("error {0},  transferred {1}", e.SocketError, e.BytesTransferred));
                close_clientsocket(token);
            }

        }
    }
}
