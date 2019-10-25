using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace FreeNet
{
    public class CUserToken
    {
        // 바이트를 패킷 형식으로 해석해주는 해석기.
        CMessageResolver message_resolver;


        // 전송할 패킷을 보관해놓는 큐. 1-Send로 처리하기 위한 큐이다.
        Queue<CPacket> sending_queue;

        // sending_queue lock처리에 사용되는 객체.
        private object cs_sending_queue;


        void on_receive(byte[] buffer , int offset , int transferred)
        {
            this.message_resolver.on_receive(buffer, offset, transferred);
        }


        // 패킷을 전송한다.
        // 큐가 비어 있을 경우에는 큐에 추가한 뒤 바로 SendAsync 매소드를 호출하고,
        // 데이터가 들어있을 경우에는 새로 추가만 한다.

        // 큐잉된 패킷의 전송 시점 
        // 현재 진행중인 SendAsync가 완료되었을 때 큐를 검사하여 나머지 패킷을 전송합니다.


        public void send(CPacket msg)
        {
            lock (this.cs_sending_queue)
            {
                // 큐가 비어 있다면 큐에 추가하고 바로 비동기 전송 매소드를 호출합니다.
                if (this.sending_queue.Count <= 0)
                {
                    this.sending_queue.Enqueue(msg);
                    start_send();
                    return;
                }

                // 큐에 무언가가 들어 있다면 아직 이전 전송이 완료되지 않은 상태이므로 큐에 추가만 하고 리턴합니다.
                // 현재 수행중인 SendAsync가 완료된 이후에 큐를 검사하여 데이터가 있으면 SendAsync를 호출하여 전송해 줄 것이다.
                this.sending_queue.Enqueue(msg);
            }
        }

        // 비동기 전송 시작
        private void start_send()
        {
            lock (this.cs_sending_queue)
            {
                // 전송이 아직 완료된 상태가 아니므로 데이터만 가져오고 큐에서 제거하진 않는다.
                CPacket msg = this.sending_queue.Peek();

                // 헤더의 패킷 사이즈를 기록합니다.
                msg.record_size();

                // 이번에 보낼 패킷 사이즈 만큼 버퍼 크기를 설정하고
                this.send_args.SetBuffer(this.send_event_args.Offset, msg.position);

                // 패킷 내용을 SocketAsyncEventArgs버퍼에 복사합니다.
                Array.Copy(msg.buffer, 0, this.send_event_args.Buffer, this.send_event_args.Offset, msg.position);

                // 비동기 전송 시작
                bool pending = this.socket.SendAsync(this.send_event_args);

                if (!pending)
                {
                    process_send(this.send_event_args);
                }
            }
        }

        public void process_send(SocketAsyncEventArgs e)
        {

            // 전송 완료된 패킷을 큐에서 제거합니다.
            CPacket packet = this.sending_queue.Dequeue();
            CPacket.destroy(packet);

            // 아직 전송하지 않은 대기중인 패킷이 있다면 다시한번 전송을 요청합니다.
            if (this.sending_queue.Count > 0)
            {
                start_send();
            }
        }
    }
}
