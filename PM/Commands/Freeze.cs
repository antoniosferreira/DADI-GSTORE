using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace PM.Commands
{
    class Freeze : Command
    {
        private readonly PM PuppetMaster;

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
                if (!match.Success)
                {
                    Console.WriteLine(">>> FAILED to parse command Crash");
                    return; 
                }

                string serverID = match.Groups["serverID"].Value;
                try
                {
                    PuppetMaster.NodesCommunicator.GetServerClient(serverID).Freeze(new Empty { });
                }
                catch (Exception)
                {
                    Console.WriteLine(">>> Failed to free server " + serverID);
                }
            });
        }
    }
}
