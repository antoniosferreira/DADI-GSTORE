using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.RegularExpressions;


namespace PM.Commands
{
    class ReplicationFactor : Command
    {
        private PM PuppetMaster;

        public ReplicationFactor(PM pm)
        {
            PuppetMaster = pm;
            Description = "ReplicationFactor <numberServers>";
            Rule = new Regex(
                @"ReplicationFactor (?<numberServers>\d+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Match match = Rule.Match(input);

            try
            {
                int numberServers = int.Parse(match.Groups["numberServers"].Value);
                List<string> serversID = PuppetMaster.NodesCommunicator.GetAllServersIDs();

                foreach (string serverID in PuppetMaster.NodesCommunicator.GetAllServersIDs()) {

                    List<string> serversToReplicate = new List<string>();

                    int pos = serversID.IndexOf(serverID);
                    int number = 0;

                    do
                    {
                        pos++;
                        if (pos >= serversID.Count)
                            pos = 0;

                        serversToReplicate.Add(serversID[pos]);
                        number++;
                    } while (number < numberServers);

                    PartitionRequest request = new PartitionRequest{ PartitionID = 'P' + serverID.Remove(0, 1) };
                    request.Servers.Add(serversToReplicate);
                    PuppetMaster.NodesCommunicator.GetServerClient(serverID).Partition(request);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }


  
    }
}
