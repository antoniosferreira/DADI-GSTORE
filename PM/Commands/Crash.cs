using System;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;


namespace PM.Commands
{
    class Crash : Command
    {
        private PM PuppetMaster;

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
            Match match = Rule.Match(input);
            try
            {
                string serverID = match.Groups["serverID"].Value;
                PuppetMaster.NodesCommunicator.GetServerClient(serverID).Crash(new Empty { });
            }
            catch (Grpc.Core.RpcException e)
            {
                return;
            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
