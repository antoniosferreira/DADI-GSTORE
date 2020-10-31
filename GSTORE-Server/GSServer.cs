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
        public int Delay { get { return _delay; } }
        private readonly int _delay;


        // Storage Object
        public StorageServer StorageServer { get; }


        public GSServer(string serverID, string serverURL, int serverPort, int minDelay, int maxDelay)
        {
            Console.WriteLine("#####   CONFIGURING GSTORE-SERVER   #####");
            Console.WriteLine("ServerID: " + serverID);
            Console.WriteLine("ServerURL:" + serverURL + " ServerPort:" + serverPort);
            _serverID = serverID;
            _serverURL = serverURL;
            _serverPort = serverPort;
            _delay = (new Random()).Next(minDelay, maxDelay + 1);
             StorageServer = new StorageServer(ServerID, Delay);
        }


        public void Start()
        {
            Console.WriteLine("##### GSTORE-SERVER RUNNING  #####");
           
            // Inits Services
            Server server = new Server
            {
                Services = { StorageServerServices.BindService(new StorageServerService(this)),
                            ServerServices.BindService(new ServerService(this)),
                            ServerCommunicationServices.BindService(new ServerCommunicationService(this))},
                Ports = { new ServerPort(ServerURL, ServerPort, ServerCredentials.Insecure) }
            };
            server.Start();
            
            Console.WriteLine("Press any key to stop...");
            
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
