using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace PM.Commands
{
    class Partition : Command
    {
        private readonly PM PuppetMaster;

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
            Task.Run(() =>
            {
                Match match = Rule.Match(input);

                if (!match.Success)
                {
                    Console.WriteLine(">>> FAILED to parse command Partition");
                    return;
                }
                string pid = match.Groups["partName"].Value;

                try {
                    // Parses server list
                    string[] parameters = input.Split(" ");
                    List<string> serversToReplicate = new List<string>();
                    for (int i = 0; i < int.Parse(parameters[1]); i++)
                        serversToReplicate.Add(parameters[3 + i]);

                    // Stores partition information on PM
                    PuppetMaster.Partitions.AddOrUpdate(pid, serversToReplicate, (key, oldValue) => serversToReplicate);
                
                } catch (Exception) {
                    Console.WriteLine(">>> FAILED to execute command Partition");
                }
            });
        }
    }
}
