using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grpc.Net.Client;
using NodesConfigurator;

namespace PM
{
    class NodesCommunicator

    {
        private Dictionary<string, (bool, ServerServices.ServerServicesClient)> Servers = new Dictionary<string, (bool, ServerServices.ServerServicesClient)>();
        private Dictionary<string, ServerServices.ServerServicesClient> Clients = new Dictionary<string, ServerServices.ServerServicesClient>();

        private Nodes Nodes = new Nodes();
        public NodesCommunicator()
        {
            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);

                Servers.Add(kvp.Key, (false, new ServerServices.ServerServicesClient(channel)));
            }

            // Establishes connection with all Clients
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllClients())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);

                Clients.Add(kvp.Key, new ServerServices.ServerServicesClient(channel));
            }
        }

        public void ActivateServer(string id) {
            Servers[id] = (true, Servers[id].Item2);
        }


        public ServerServices.ServerServicesClient GetServerClient(string id)
        {
            if (Servers[id].Item1)
                return Servers[id].Item2;

            return null;
        }

        public List<ServerServices.ServerServicesClient> GetAllServersClients()
        {
            return Servers.Values.Where(t=>t.Item1).Select(t => t.Item2).ToList();
        }

        public List<string> GetAllServersIDs()
        {
            return Servers.Keys.Where(t=> Servers[t].Item1).ToList();
        }

        public List<ServerServices.ServerServicesClient> GetAllClients()
        {
            return Clients.Values.ToList();
        }
    }
}
