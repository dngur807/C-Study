using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace FreeNet
{
    class NetworkService
    {
        // 클라이언트의 접속을 받아들이기 위한 객체
        CListener client_listener;

        // 메시지 수신, 전송시 필요한 오브젝트입니다.
        SocketAsyncEventArgsPool receive_event_args_pool;
        SocketAsyncEventArgsPool send_event_args_pool;

        // 메시지 수신 전송시 .Net 비동기 소켓에서 사용할 버퍼를 관리하는 객체
        BufferManager buffer_manager;

        // 클라이언트의 접속이 이루어 졌을 때 호출되는 델리게이트 입니다.
        public delegate void SessionHandler(CUserToken token);
        public SessionHandler session_created_callback { get; set; }

        // 최대 유저 연결 수
        private int max_connections;
        private int buffer_size;
        readonly int pre_alloc_count = 2;		// read, write

        public NetworkService()
        {
            this.max_connections = 1000;
            this.buffer_size = 1024;

            this.receive_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);
            this.send_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);
            this.buffer_manager = new BufferManager(this.max_connections * this.buffer_size * this.pre_alloc_count, this.buffer_size);

            SocketAsyncEventArgs args;
            for (int i = 0; i < this.max_connections; i++)
            {
                CUserToken token = new CUserToken();

                // recvPool
                {
                    args = new SocketAsyncEventArgs();
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
                    args.UserToken = token;
                    this.buffer_manager.SetBuffer(args);
                    this.receive_event_args_pool.Push(args);// 동기화
                }

                //sendPool
                {
                    args = new SocketAsyncEventArgs();
                    args.Completed += new EventHandler<SocketAsyncEventArgs>(send_completed);
                    args.UserToken = token;
                    this.buffer_manager.SetBuffer(args);
                    this.send_event_args_pool.Push(args);// 동기화
                }
            }
        }
        public void listen(string host, int port, int backlog)
        {
            this.client_listener = new CListener();
            this.client_listener.callback_on_newclient += on_new_client;
            this.client_listener.start(host, port, backlog);
        }

        void on_new_client(Socket client_socket , object sender)
        {
            SocketAsyncEventArgs recv_args =  this.receive_event_args_pool.Pop();
            SocketAsyncEventArgs send_args = this.send_event_args_pool.Pop();

            // SocketAsyncEventARgs 를 생성할 때 만들어 두었던 CUserToken을 꺼내와서
            // 콜백 매소드의 파라미터로 넘겨 줍니다.
            if (this.session_created_callback != null)
            {
                CUserToken user_token = recv_args.UserToken as CuserToken;
                this.session_created_callback(user_token);
            }

            // 이제 클라이언트로 부터 데이터를 수신할 준비를 합니다.
            begin_receive(client_socket, recv_args, send_args);
        }

        void begin_receive(Socket socket , SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
        {
            // receive_args , send_args 아무곳에서 꺼내와도 된다 둘다 동일한 CUserToken 을 물고 있다.
            CUserToken token = receive_args.UserToken as CUserToken;
            token.set_event_args(receive_args, send_args);

            // 생성된 클라이언트 소켓을 보관해 놓고 통신할 때 사용한다.  
            token.socket = socket;

            // 데이터를 받을 수 있도록 소켓 매소드를 호출해준다.  
            // 비동기로 수신할 경우 워커 스레드에서 대기중으로 있다가 Completed에 설정해놓은 매소드가 호출된다.  
            // 동기로 완료될 경우에는 직접 완료 매소드를 호출해줘야 한다.  
            bool pending = socket.ReceiveAsync(receive_args);
            if (!pending)
            {
                process_receive(receive_args);
            }
        }

        void receive_completed(object sender , SocketAsyncEventArgs e)
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
                // 이후 작업은 CUserToken에 맡긴다.
                token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);
                // 다음 메시지 수신을 위해서 다시 ReceiveAsync 매소드를 호출합니다.
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
