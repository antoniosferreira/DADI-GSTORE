using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace PM.Commands
{
    class Server : Command
    {
        private readonly PCSServices.PCSServicesClient PCSClient;
        private readonly PM PuppetMaster;

        public Server(PCSServices.PCSServicesClient client, PM pm)
        {
            PCSClient = client;
            PuppetMaster = pm;
            Description = "Server <serverID> <serverURL> <minDelay> <maxDelay>";
            Rule = new Regex(
                @"Server (?<sid>\w+)\s+(?<URL>[^ ]+)\s+(?<mind>\d+)\s+(?<maxd>\d+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Task.Run(() =>
            {
                Match match = Rule.Match(input);

                if (!match.Success) {
                    Console.WriteLine(">>> FAILED to parse Server command;");
                    return;
                }

                string serverID = match.Groups["sid"].Value;
                string serverURL = match.Groups["URL"].Value;
                int minDelay = int.Parse(match.Groups["mind"].Value);
                int maxDelay = int.Parse(match.Groups["maxd"].Value);

                try
                {
                    // Launches Server
                    PCSClient.InitServerAsync(
                        new ServerRequest
                        {
                            ServerID = serverID,
                            ServerURL = serverURL,
                            MinDelay = minDelay,
                            MaxDelay = maxDelay
                        });

                    PuppetMaster.NodesCommunicator.ActivateServer(serverID);
                    Console.WriteLine(">>> Server {0} launched", serverID);

                    Thread.Sleep(200);

                    // Launches partitions
                    foreach (KeyValuePair<string,List<string>> kvp in PuppetMaster.Partitions)
                    {
                        if (kvp.Value.Contains(serverID))
                        {
                            PartitionRequest request = new PartitionRequest { PartitionID = kvp.Key };
                            request.Servers.Add(kvp.Value);
                            
                            PuppetMaster.NodesCommunicator.GetServerClient(serverID).Partition(request);
                        }
                    }
                    Console.WriteLine(">>> Replications on server {0} are launched", serverID);


                }
                catch (Exception)
                {
                    Console.WriteLine(">>> FAILED to execute server command" + serverID);
                }
            });
        }
    }
}
