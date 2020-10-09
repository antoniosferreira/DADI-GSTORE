using System;
using Grpc.Core;

using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;


namespace PCS
{
    class Program
    {
        const int Port = 10000;

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Failed to initiate the PCS module");
                Console.WriteLine("Usage: <nodesFile> <clientScripts>");

                Environment.Exit(1);
            }

            Server server = new Server
            {
                Services = { PCSServices.BindService(new PCSService()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };


            server.Start();

            Console.WriteLine("PCS initiated on " + Port);
            Console.WriteLine("Press any key to stop the PCS server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();
        }
    }
}
