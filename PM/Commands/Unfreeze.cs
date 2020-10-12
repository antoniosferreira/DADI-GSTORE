using System;
using System.Text.RegularExpressions;


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
            Match match = Rule.Match(input);
            try
            {
                string serverID = match.Groups["serverID"].Value;
                PuppetMaster.NodesCommunicator.GetServerClient(serverID).Unfreeze(new Empty { });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
