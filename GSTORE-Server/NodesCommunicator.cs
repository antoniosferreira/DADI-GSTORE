using System;
using System.Collections.Generic;
using NodesConfigurator;
using Grpc.Net.Client;
using System.Linq;


namespace GSTORE_Server
{
    class NodesCommunicator
    {

        private readonly Dictionary<string, ServerCommunicationServices.ServerCommunicationServicesClient> Servers = new Dictionary<string, ServerCommunicationServices.ServerCommunicationServicesClient>();

        // Reads All nodes from config files
        private readonly Nodes Nodes = new Nodes();

        public NodesCommunicator()
        {
            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);
                Servers.Add(kvp.Key, new ServerCommunicationServices.ServerCommunicationServicesClient(channel));
            }
        }

        public string GetServerIDAtIndex(int index)
        {
            return Servers.Keys.ToList()[index];
        } 

        public ServerCommunicationServices.ServerCommunicationServicesClient GetServerClient(string id)
        {
            return Servers[id];
        }

        public List<ServerCommunicationServices.ServerCommunicationServicesClient> GetAllServers()
        {
            return Servers.Values.ToList();
        }
    }
}
