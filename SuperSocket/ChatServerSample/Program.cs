using System;

namespace ChatServerSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //  var serverOption = ParseCommandLine(args);
            var serverOption = new ChatServerOption();
            serverOption.Name = "ChatServer";
            serverOption.Port = 32452;
            serverOption.MaxConnectionNumber = 256;
            serverOption.MaxRequestLength = 1024;
            serverOption.ReceiveBufferSize = 16384;
            serverOption.SendBufferSize = 16384;
            serverOption.RoomMaxCount = 100;
            serverOption.RoomMaxUserCount = 4;
            serverOption.RoomStartNumber = 0;

            if (serverOption == null)
            {
                return; 
            }

            var serverApp = new MainServer();
            serverApp.InitConfig(serverOption);

            serverApp.CreateStartServer();
        }

        static ChatServerOption ParseCommandLine(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<ChatServerOption>(args) as CommandLine.Parsed<ChatServerOption>;

            if (result == null)
            {
                System.Console.WriteLine("Failed Command Line");
                return null;
            }
            return result.Value;
        }
    }
}
