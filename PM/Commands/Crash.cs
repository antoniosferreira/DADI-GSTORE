using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace PM.Commands
{
    class Crash : Command
    {
        private readonly PM PuppetMaster;

        public Crash(PM pm)
        {
            PuppetMaster = pm;

            Description = "Crash <serverID>";
            Rule = new Regex(
                @"Crash (?<serverID>\w+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Task.Run(() => {

                Match match = Rule.Match(input);
                if (!match.Success)
                {
                    Console.WriteLine(">>> FAILED to parse command Crash");
                    return; 
                }
                
                string serverID = match.Groups["serverID"].Value;

                try
                {
                    PuppetMaster.NodesCommunicator.GetServerClient(serverID).Crash(new Empty { });
                }
                catch (Exception) {
                    Console.WriteLine(">>> Crashed server " + serverID);
                }
            });
        }
    }
}
