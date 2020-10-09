using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;


namespace GSTORE_Server
{
    public class GSServer
    {
        // Server Configuration Information
        public string ServerID { get { return _serverID; } }
        private string _serverID;
        public string ServerURL { get { return _serverURL; } }
        private string _serverURL;
        public int ServerPort { get { return _serverPort; } }
        private int _serverPort;
        public int MinDelay { get { return _minDelay; } }
        private int _minDelay;
        public int MaxDelay { get { return _maxDelay; } }
        private int _maxDelay;

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
            Server server = new Server
            {
                Services = { GStoreServerServices.BindService(new GStoreServerService()) },
                Ports = { new ServerPort(ServerURL, ServerPort, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();


            Console.WriteLine("Press any key to stop...");
            server.ShutdownAsync().Wait();
        }
    }
}
