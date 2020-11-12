using System;
using System.Collections.Generic;
using NodesConfigurator;
using Grpc.Net.Client;
using System.Linq;


using System.Threading;


namespace GSTORE_Client.Communication
{
    class NodesCommunicator
    {
        private readonly Dictionary<string, (bool, StorageServerServices.StorageServerServicesClient)> Servers = new Dictionary<string, (bool, StorageServerServices.StorageServerServicesClient)>();

        // Reads All nodes from config files
        private readonly Nodes Nodes = new Nodes();

        public NodesCommunicator()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {                
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);

                // All servers are active by default
                Servers.Add(kvp.Key, (true, new StorageServerServices.StorageServerServicesClient(channel)));
            }

        }

        public void DeactivateServer(string id)
        {
            Servers[id] = (false, Servers[id].Item2);
        }

        public int GetServersCounter()
        {
            return Servers.Where(x => x.Value.Item1).ToList().Count;
        }

        public StorageServerServices.StorageServerServicesClient GetServerClient(string id)
        {         
            return Servers.Where(x => x.Key.Equals(id) && x.Value.Item1).Select(t => t.Value.Item2).First();
        }

        public List<StorageServerServices.StorageServerServicesClient> GetAllServers()
        {
            return Servers.Where(x => x.Value.Item1).Select(x => x.Value.Item2).ToList();
        }
        public List<string> GetAllServersID()
        {
            return Servers.Where(x => x.Value.Item1).Select(x => x.Key).ToList();
        }
    }
}
