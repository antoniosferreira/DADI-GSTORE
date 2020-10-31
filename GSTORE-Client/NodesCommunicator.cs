using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using NodesConfigurator;
using Grpc.Net.Client;
using System.Linq;


namespace GSTORE_Client
{
    class NodesCommunicator
    {

        private readonly ConcurrentDictionary<string, StorageServerServices.StorageServerServicesClient> Servers = new ConcurrentDictionary<string, StorageServerServices.StorageServerServicesClient>();

        // Reads All nodes from config files
        private readonly Nodes Nodes = new Nodes();


        public NodesCommunicator()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {
                Console.WriteLine("key:" + kvp.Key + "VALUE:" + kvp.Value);
                
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);
                Servers.TryAdd(kvp.Key, new StorageServerServices.StorageServerServicesClient(channel));
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
