using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace PM.Commands
{
    class Freeze : Command
    {
        private PM PuppetMaster;

        public Freeze(PM pm)
        {
            PuppetMaster = pm;

            Description = "Freeze <serverID>";
            Rule = new Regex(
                @"Freeze (?<serverID>\w+).*",
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
                    PuppetMaster.NodesCommunicator.GetServerClient(serverID).Freeze(new Empty { });
                }
                catch (Exception e)
                {
                    Console.WriteLine(">>> Failed to free server " + serverID);
                }
            });
        }
    }
}
