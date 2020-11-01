using System;
using System.Collections.Generic;
using NodesConfigurator;
using Grpc.Net.Client;
using System.Linq;


namespace GSTORE_Server
{
    class NodesCommunicator
    {

        private readonly List<(string, ServerCommunicationServices.ServerCommunicationServicesClient)> Servers = new List<(string, ServerCommunicationServices.ServerCommunicationServicesClient)>();
        // Reads All nodes from config files
        private readonly Nodes Nodes = new Nodes();

        public NodesCommunicator()
        {
            // Establishes connection with all servers
            foreach (KeyValuePair<string, string> kvp in Nodes.GetAllServers())
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                GrpcChannel channel = GrpcChannel.ForAddress(kvp.Value);
                Servers.Add((kvp.Key, new ServerCommunicationServices.ServerCommunicationServicesClient(channel)));
            }

            Servers.Sort();

        }

        public string GetServerIDAtIndex(int index)
        {
            return Servers[index].Item1;
        } 

        public ServerCommunicationServices.ServerCommunicationServicesClient GetServerClient(string id)
        {
            return Servers.Where(x => x.Item1.Equals(id)).First().Item2;
        }

        public List<ServerCommunicationServices.ServerCommunicationServicesClient> GetAllServers()
        {
            return (List<ServerCommunicationServices.ServerCommunicationServicesClient>)Servers.Select(x => x.Item2);
        }
    }
}
