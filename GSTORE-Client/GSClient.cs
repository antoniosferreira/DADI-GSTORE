using System;
using Grpc.Net.Client;

namespace GSTORE_Client
{
    class GSClient
    {
        // Client Configuration Information
        public string ClientID { get { return _clientID; } }
        private readonly string _clientID;
        public string ClientURL { get { return _clientURL; } }
        private readonly string _clientURL;
        public string ClientScript { get { return _clientScript; } }
        private readonly string _clientScript;
    
    
        public GSClient(string clientID, string clientURL, string clientScript)
        {
            _clientID = clientID;
            _clientURL = clientURL;
            _clientScript = clientScript;

        }

        public void Start()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress("http://localhost:1000");
            StorageServerServices.StorageServerServicesClient client = new StorageServerServices.StorageServerServicesClient(channel);

            var reply = client.Read(
                new ReadRequest
                {
                    Key="123"
                });
                
            Console.WriteLine(reply.Value);
            var reply2 = client.Write(
                new WriteRequest
                {
                    Key="123",
                    Value="123"
                });
            Console.WriteLine(reply2.Success);
        }
    }
}
