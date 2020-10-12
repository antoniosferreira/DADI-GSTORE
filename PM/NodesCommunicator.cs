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
        private Dictionary<string, ServerServices.ServerServicesClient> Servers = new Dictionary<string, ServerServices.ServerServicesClient>();
        private Dictionary<string, ServerServices.ServerServicesClient> Clients = new Dictionary<string, ServerServices.ServerServicesClient>();


        private Nodes Nodes = new Nodes();
        public NodesCommunicator()
        {
            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);

                Servers.Add(kvp.Key, new ServerServices.ServerServicesClient(channel));
            }

            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllClients())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);

                Clients.Add(kvp.Key, new ServerServices.ServerServicesClient(channel));
            }
        }

        public ServerServices.ServerServicesClient GetServerClient(string id)
        {
            return Servers[id];
        }

        public List<ServerServices.ServerServicesClient> GetAllServers()
        {
            return Servers.Values.ToList();
        }

        public List<ServerServices.ServerServicesClient> GetAllClients()
        {
            return Clients.Values.ToList();
        }
    }
}
