using System;
using System.Text.RegularExpressions;
using Grpc.Core;
using Grpc.Net.Client;


namespace GSTORE_Client.Commands
{
    class ListServer : Command
    {
        public ListServer(Client client)
        {
            Client = client;

            Description = "listServer <serverID>";
            Rule = new Regex(
                @"listServer (?<serverID>\w+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Match match = Rule.Match(input);

            if (!match.Success)
            {
                Console.WriteLine(">>> FAILED to parse command ListServer");
                return;
            }

            string serverID = match.Groups["serverID"].Value;

            try
            {
                ListServerReply reply = Client.NodesCommunicator.GetServerClient(serverID).ListServer(new ListServerRequest { });
                foreach (string s in reply.Listings)
                    Console.WriteLine(s);
            }
            catch (RpcException)
            {
                Console.WriteLine(">>> Server %s failed", serverID);
                Client.NodesCommunicator.DeactivateServer(serverID);
            }
            catch (Exception)
            {
                Console.WriteLine(">>> Failed to execute listserver ", serverID);
            }

        }
    }
}
