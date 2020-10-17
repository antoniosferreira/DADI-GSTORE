using System;
using System.Collections.Generic;
using NodesConfigurator;
using Grpc.Net.Client;
using System.Linq;


namespace GSTORE_Client
{
    class NodesCommunicator
    {

        private readonly Dictionary<string, StorageServerServices.StorageServerServicesClient> Servers = new Dictionary<string, StorageServerServices.StorageServerServicesClient>();

        // Reads All nodes from config files
        private readonly Nodes Nodes = new Nodes();


        public NodesCommunicator()
        {
            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);
                Servers.Add(kvp.Key, new StorageServerServices.StorageServerServicesClient(channel));
            }
        }

        public string GetServerIDAtIndex(int index)
        {
            return Servers.Keys.ToList()[index];
        } 

        public StorageServerServices.StorageServerServicesClient GetServerClient(string id)
        {
            return Servers[id];
        }

        public List<StorageServerServices.StorageServerServicesClient> GetAllServers()
        {
            return Servers.Values.ToList();
        }
    }
}
