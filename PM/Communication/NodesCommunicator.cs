using System;
using System.Collections.Generic;
using System.Linq;
using Grpc.Net.Client;
using NodesConfigurator;

namespace PM.Communication
{
    class NodesCommunicator

    {
        // Nodes information
        private readonly Dictionary<string, (bool, PMServices.PMServicesClient)> Servers = new Dictionary<string, (bool, PMServices.PMServicesClient)>();
        private readonly Dictionary<string, StatusService.StatusServiceClient> Clients = new Dictionary<string, StatusService.StatusServiceClient>();

        // Reads data from file
        private readonly Nodes Nodes = new Nodes();


        public NodesCommunicator()
        {
            // Creates Client for All Servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);

                Servers.Add(kvp.Key, (false, new PMServices.PMServicesClient(channel)));
            }

            // Creates Client for All Clients
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllClients())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);

                Clients.Add(kvp.Key, new StatusService.StatusServiceClient(channel));
            }
        }


        // Server is on, client is safe to use
        public void ActivateServer(string id) {
            Servers[id] = (true, Servers[id].Item2);
        }
        public void DeactivateServer(string id)
        {
            Servers[id] = (false, Servers[id].Item2);
        }


        public PMServices.PMServicesClient GetServerClient(string id)
        {
            if (Servers[id].Item1)
                return Servers[id].Item2;

            return null;
        }


        public List<PMServices.PMServicesClient> GetAllServersClients()
        {
            return Servers.Values.Where(t=>t.Item1).Select(t => t.Item2).ToList();
        }


        public List<StatusService.StatusServiceClient> GetAllClients()
        {
            return Clients.Values.ToList();
        }


        public List<string> GetAllServersIDs()
        {
            return Servers.Keys.Where(t=> Servers[t].Item1).ToList();
        }
    }
}
