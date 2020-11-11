using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace PM.Commands
{
    class Unfreeze : Command
    {
        private readonly PM PuppetMaster;

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
                if (!match.Success)
                {
                    Console.WriteLine(">>> FAILED to parse command Unfreeze");
                    return;
                }

                string serverID = match.Groups["serverID"].Value;
                try
                {
                    PuppetMaster.NodesCommunicator.GetServerClient(serverID).Unfreeze(new Empty { });
                }
                catch (Exception)
                {
                    Console.WriteLine(">>> FAILED to unfreeze server " + serverID);
                }
            });
        }
    }
}
