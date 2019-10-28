using System;


/**
 * SuperSocket 구조
 * 
 * ReceiveFilter<T>
 * 가장 처음으로 메시지가 도착하는 곳입니다.
 * 해당 부분에서 하는 일은 메시지의 길이를 확인하고 더받을 필요가 있는지 결정합니다.
 * 또한 메시지를 다 받으면 RequestInfo를 만드는 작업을 합니다.
 * 
 * RequestInfo<T>
 * 서버에서 온 메시지라고 볼 수 있고 ReceiveFilter에서 만들어 졌습니다.
 * 
 * 
 * AppServer<T, RequestInfo>
 * 서버의 메인 부분으로 새로운 세션이 서버에 연결하였을 떄 
 * 메시지를 받았을 때 연결이 끊어질 때 상황에 호출할 메서드를 연결 할 수 있습니다.
 * 
 * AppSession<T, RequestInfo>
 * 서버에 들어온 유저라고 생각하면 됩니다.
 * 하나의 AppSession은 하나의 유저입니다.
*/

namespace EchoServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var serverOption = ParseCommandLine(args);
            if (serverOption == null)
            {
                return;
            }

            var server = new MainServer();
            server.InitConfig(serverOption);
            server.CreateServer();

            var IsResult = server.Start();

            if (IsResult)
            {
                MainServer.MainLogger.Info("서버 네트워크 시작");
            }
            else
            {
                Console.WriteLine("서버 네트워크 시작 실패");
                return;
            }

            Console.WriteLine("Key를 누르면 종료합니다...");
            Console.ReadKey();
        }

        static ServerOption ParseCommandLine(string[] args)
        {
            var option = new ServerOption
            {
                Port = 32452,
                MaxConnectionNumber = 32,
                Name = "EchoServer"
            };
            return option;
        }
    }

    public class ServerOption
    {
        public int Port { get; set; }
        public int MaxConnectionNumber { get; set; } = 0;
        public string Name { get; set; }
    }
}
