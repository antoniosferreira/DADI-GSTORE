using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace PM.Commands
{
    class Partition : Command
    {
        private PM PuppetMaster;

        public Partition(PM pm)
        {
            PuppetMaster = pm;

            Description = "Partition <numberServers> <partitionName> <serverID...>";
            Rule = new Regex(
                @"Partition (?<number>\d+)\s+(?<partName>[^ ]+)\s+(?<serversID>[^ ]+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            
            try
            {
                string[] parameters = input.Split(" ");
                List<string> serversToReplicate = new List<string>();

                for (int i=0; i < int.Parse(parameters[1]); i++)
                    serversToReplicate.Add(parameters[3 + i]);

                PartitionRequest request = new PartitionRequest { PartitionID = 'P' + parameters[2].Remove(0, 1) };
                request.Servers.Add(serversToReplicate);
                PuppetMaster.NodesCommunicator.GetServerClient(parameters[2]).Partition(request);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
