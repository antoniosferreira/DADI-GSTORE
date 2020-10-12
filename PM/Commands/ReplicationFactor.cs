using System;
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
                @"ReplicationFactor (?<n>\d+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Console.Write("Command Not yet Implemented:");
            Console.WriteLine(input);
            //Console.WriteLine("Failed to execute command:" + input);
        }
    }
}
