using System;
using System.Text.RegularExpressions;


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
            Match match = Rule.Match(input);
            try
            {
                string serverID = match.Groups["serverID"].Value;
                PuppetMaster.NodesCommunicator.GetServerClient(serverID).Freeze(new Empty { });
            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
