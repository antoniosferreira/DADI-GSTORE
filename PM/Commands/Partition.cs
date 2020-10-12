using System;
using System.Text.RegularExpressions;


namespace PM.Commands
{
    class Partition : Command
    {
        private PM PuppetMaster;

        public Partition(PM pm)
        {
            PuppetMaster = pm;

            Description = "ReplicationFactor <numberServers> <partitionName> <serverID...>";
            Rule = new Regex(
                @"Partition (?<number>\d+)\s+(?<partName>[^ ]+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Console.WriteLine("Command Not yet Implemented");
            //Console.WriteLine("Failed to execute command:" + input);
        }
    }
}
