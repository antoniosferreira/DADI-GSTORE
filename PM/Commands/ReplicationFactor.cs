using System;
using System.Text.RegularExpressions;

namespace PM.Commands
{
    class ReplicationFactor : Command
    {
        private readonly PM PuppetMaster;

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
            if (match.Success)
            {
                int numberServers = int.Parse(match.Groups["numberServers"].Value);
                PuppetMaster.ReplicationFactor = numberServers;
            }
            else
                Console.WriteLine(">>>>> Failed to parse command ReplicationFactor");
        }  
    }
}
