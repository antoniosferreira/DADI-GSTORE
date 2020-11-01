using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


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
            int numberServers = int.Parse(match.Groups["numberServers"].Value);

            List<string> serversID = PuppetMaster.NodesCommunicator.GetAllServersIDs();
            foreach (string serverID in PuppetMaster.NodesCommunicator.GetAllServersIDs())
            {
                Task.Run(() =>
                {
                    PuppetMaster.NodesCommunicator.GetServerClient(serverID).Replication(new Empty { });
                });
            }
        
        }  
  
    }
}
