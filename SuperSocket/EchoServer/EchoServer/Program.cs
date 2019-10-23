using System;
namespace EchoServer
{
    public class Program
    {
        static void Main(String[] args)
        {
            Console.WriteLine("Hello SuperSocketLite");

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
            }

            Console.WriteLine("Key 누르면 종료합니다.");

            Console.ReadKey();
        }


        static ServerOption ParseCommandLine(string[] args)
        {
            var option = new ServerOption
            {
                Port = 12021,
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
