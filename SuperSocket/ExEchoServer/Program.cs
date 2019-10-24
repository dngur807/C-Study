using SuperSocket.SocketBase;
using SuperSocket.SocketBase.Config;
using SuperSocket.SocketBase.Protocol;
using SuperSocket.SocketEngine.Protocol;
using SuperSocket.Common;
using System;

namespace ExEchoServer
{

    public class MyReceiveFilter : FixedHeaderReceiveFilter<BinaryRequestInfo>
    {
        public MyReceiveFilter() : base(6) // 헤더의 길이
        {

        }

        protected override int GetBodyLengthFromHeader(byte[] header, int offset, int length)
        {
            return (int)header[offset + 4] + (int)header[offset + 5] * 256 - 6;
        }

        protected override BinaryRequestInfo ResolveRequestInfo(ArraySegment<byte> header, byte[] bodyBuffer, int offset, int length)
        {

            var byteTmp = bodyBuffer.CloneRange(offset,
                                                length);

            return new BinaryRequestInfo(BitConverter.ToUInt32(header.Array, 0 ).ToString() , MyReceiveFilter.Combine(header.Array, byteTmp));
        }

        public static byte[] Combine(byte[] first , byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }

    // AppServer
    public class MyAppServer : AppServer<MyAppSession , BinaryRequestInfo>
    {
        public MyAppServer() : base(new DefaultReceiveFilterFactory<MyReceiveFilter , BinaryRequestInfo>())
        {
            this.NewSessionConnected += new SessionHandler<MyAppSession>(MyServer_NewSessionConnected);
            this.SessionClosed += new SessionHandler<MyAppSession, CloseReason>(MyServer_SessionClosed);
            this.NewRequestReceived += new RequestHandler<MyAppSession, BinaryRequestInfo>(MyServer_NewRequestReceived);

        }

        private void MyServer_NewRequestReceived(MyAppSession session, BinaryRequestInfo requestinfo)
        {
            Console.WriteLine("MyServer_NewRequestReceived");

            session.Send(requestinfo.Body, 0, requestinfo.Body.Length);
        }

        private void MyServer_SessionClosed(MyAppSession session, CloseReason value)
        {
            Console.WriteLine("MySErver_UserClosed");
        }

        private void MyServer_NewSessionConnected(MyAppSession session)
        {
            Console.WriteLine("MyServer_NewUserConnected");
        }
    }

    // AppSession
    public class MyAppSession : AppSession<MyAppSession , BinaryRequestInfo>
    {

    }

    public class Program
    {
        static void Main(String[] args)
        {
            MyAppServer server = new MyAppServer();

            server.Setup(new RootConfig(), new ServerConfig()
            {
                Port = 3000,
                Ip = "Any",
            });

            server.Start();
            while (Console.ReadLine() != "q")
            {

            }
        }
    }
}
