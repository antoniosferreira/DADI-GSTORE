using System;
using Grpc.Core;
using GSTORE_Server.Storage;


namespace GSTORE_Server
{
    public class Server
    {

        // Server Configuration Information
        public string ServerID { get; }
        public string ServerURL { get; }
        public int ServerPort { get; }
        public int Delay { get; }


        // Storage Object
        public StorageServer StorageServer { get; }


        public Server(string serverID, string serverURL, int serverPort, int minDelay, int maxDelay)
        {
            Console.WriteLine("#####   CONFIGURING GSTORE-SERVER   #####");
            Console.WriteLine("ServerID: " + serverID);
            Console.WriteLine("ServerURL:" + serverURL + " ServerPort:" + serverPort);
            ServerID = serverID;
            ServerURL = serverURL;
            ServerPort = serverPort;
            Delay = (new Random()).Next(minDelay, maxDelay + 1);
            StorageServer = new StorageServer(ServerID, Delay);
        }


        public void Start()
        {
            Console.WriteLine("##### GSTORE-SERVER RUNNING  #####");

            // Inits Services
            Grpc.Core.Server server = new Grpc.Core.Server
            {
                Services = { StorageServerServices.BindService(new StorageServerService(this.StorageServer)),
                            PMServices.BindService(new PMService(this.StorageServer)),
                            ServerCommunicationServices.BindService(new ServerCommunicationService(this.StorageServer))},
                Ports = { new ServerPort(ServerURL, ServerPort, ServerCredentials.Insecure) }
            };
            server.Start();
            
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }


    }
}
