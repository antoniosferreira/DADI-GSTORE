using System;
using Grpc.Net.Client;

namespace GSTORE_Client
{
    class GSClient
    {
        // Client Configuration Information
        public string ClientID { get { return _clientID; } }
        private string _clientID;
        public string ClientURL { get { return _clientURL; } }
        private string _clientURL;
        public string ClientScript { get { return _clientScript; } }
        private string _clientScript;
    
    
        public GSClient(string clientID, string clientURL, string clientScript)
        {
            _clientID = clientID;
            _clientURL = clientURL;
            _clientScript = clientScript;

        }

        public void Start()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:1001");
            GStoreServerServices.GStoreServerServicesClient client = new GStoreServerServices.GStoreServerServicesClient(channel);

        }
    }
}
