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

        private readonly List<(string, StorageServerServices.StorageServerServicesClient)> Servers = new List<(string, StorageServerServices.StorageServerServicesClient)>();

        // Reads All nodes from config files
        private readonly Nodes Nodes = new Nodes();

        public NodesCommunicator()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {                
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);
                Servers.Add((kvp.Key, new StorageServerServices.StorageServerServicesClient(channel)));
            }

            Servers.Sort();
        }

        public int GetServersCounter()
        {
            return Servers.Count;
        }

        public string GetServerIDAtIndex(int index)
        {
            return Servers[index].Item1;
        }

        public StorageServerServices.StorageServerServicesClient GetServerClient(string id)
        {
            return Servers.Where(x => x.Item1.Equals(id)).Select(t => t.Item2).First();
        }

        public List<StorageServerServices.StorageServerServicesClient> GetAllServers()
        {
            return Servers.Select(x => x.Item2).ToList();
        }

        public List<string> GetAllServersID()
        {
            return Servers.Select(x => x.Item1).ToList();
        }
    }
}
