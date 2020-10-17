using System;
using Grpc.Core;
using GSTORE_Server.Storage;


namespace GSTORE_Server
{
    public class GSServer
    {
        // Server Configuration Information
        public string ServerID { get { return _serverID; } }
        private readonly string _serverID;
        public string ServerURL { get { return _serverURL; } }
        private readonly string _serverURL;
        public int ServerPort { get { return _serverPort; } }
        private readonly int _serverPort;
        public int MinDelay { get { return _minDelay; } }
        private readonly int _minDelay;
        public int MaxDelay { get { return _maxDelay; } }
        private readonly int _maxDelay;

        public GSServer(string serverID, string serverURL, int serverPort, int minDelay, int maxDelay)
        {
            Console.WriteLine("#####   CONFIGURING GSTORE-SERVER   #####");
            Console.WriteLine("ServerID: " + serverID);
            Console.WriteLine("ServerURL:" + serverURL + " ServerPort:" + serverPort);
            _serverID = serverID;
            _serverURL = serverURL;
            _serverPort = serverPort;
            _minDelay = minDelay;
            _maxDelay = maxDelay;
        }


        public void Start()
        {
            Console.WriteLine("##### GSTORE-SERVER RUNNING  #####");
           
            // Starts Storage Service to Clients 
            StorageServer storageServer = new StorageServer(ServerID);
            Server server = new Server
            {
                Services = { StorageServerServices.BindService(new StorageServerService(storageServer)),
                            ServerServices.BindService(new ServerService(storageServer)),
                            ServerCommunicationServices.BindService(new ServerCommunicationService(storageServer))},
                Ports = { new ServerPort(ServerURL, ServerPort, ServerCredentials.Insecure) }
            };
            server.Start();
            
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
