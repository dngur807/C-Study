using System;

namespace ChatServerSample
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

            var serverApp = new MainServer();
            serverApp.InitConfig(serverOption);
            serverApp.CreateServerServer();
        }
    }
}
