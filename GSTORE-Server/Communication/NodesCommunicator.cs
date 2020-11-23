using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NodesConfigurator;
using Grpc.Net.Client;
using System.Linq;


namespace GSTORE_Server.Communication
{
    class NodesCommunicator
    {

        private readonly ConcurrentDictionary<string,(bool, ServerCommunicationServices.ServerCommunicationServicesClient)> Servers = new ConcurrentDictionary<string,(bool, ServerCommunicationServices.ServerCommunicationServicesClient)>();
        // Reads All nodes from config files
        private readonly Nodes Nodes = new Nodes();

        public NodesCommunicator()
        {
            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);
                Servers.TryAdd(kvp.Key, (true, new ServerCommunicationServices.ServerCommunicationServicesClient(channel)));
            }
        }

        public void DeactivateServer(string id)
        {
            Servers[id] = (false, Servers[id].Item2);
        }

        public ServerCommunicationServices.ServerCommunicationServicesClient GetServerClient(string id)
        {
            if (Servers[id].Item1)
                return Servers[id].Item2;

            return null;
        }

        public List<ServerCommunicationServices.ServerCommunicationServicesClient> GetAllServers()
        {
            return (List<ServerCommunicationServices.ServerCommunicationServicesClient>)Servers.Where(x => x.Value.Item1).Select(x => x.Value.Item2);
        }

        public List<Tuple<string, ServerCommunicationServices.ServerCommunicationServicesClient>> GetServers()
        {
            return Servers.Select(selector: x => new Tuple<string, ServerCommunicationServices.ServerCommunicationServicesClient>(x.Key, x.Value.Item2)).ToList();
        }
    }
}
