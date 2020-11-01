using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace PM.Commands
{
    class Unfreeze : Command
    {
        private PM PuppetMaster;

        public Unfreeze(PM pm)
        {
            PuppetMaster = pm;

            Description = "Unfreeze <serverID>";
            Rule = new Regex(
                @"Unfreeze (?<serverID>\w+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Task.Run(() =>
            {
                Match match = Rule.Match(input);
                string serverID = match.Groups["serverID"].Value;
                try
                {
                    PuppetMaster.NodesCommunicator.GetServerClient(serverID).Unfreeze(new Empty { });
                }
                catch (Exception e)
                {
                    Console.WriteLine(">>> Failed to unfreeze server " + serverID);
                }
            });
        }
    }
}
