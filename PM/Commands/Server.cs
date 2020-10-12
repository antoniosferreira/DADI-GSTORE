using System;
using System.Text.RegularExpressions;

namespace PM.Commands
{
    class Server : Command
    {
        private PCSServices.PCSServicesClient PCSClient;

        public Server(PCSServices.PCSServicesClient client)
        {
            PCSClient = client;

            Description = "Server <serverID> <serverURL> <minDelay> <maxDelay>";
            Rule = new Regex(
                @"Server (?<sid>\w+)\s+(?<URL>[^ ]+)\s+(?<mind>\d+)\s+(?<maxd>\d+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Match match = Rule.Match(input);

            try
            {
                string serverID = match.Groups["sid"].Value;
                string serverURL = match.Groups["URL"].Value;
                int minDelay = int.Parse(match.Groups["mind"].Value);
                int maxDelay = int.Parse(match.Groups["maxd"].Value);

                PCSClient.InitServerAsync(
                    new ServerRequest {
                         ServerID = serverID,
                         ServerURL = serverURL,
                         MinDelay = minDelay,
                         MaxDelay = maxDelay});


            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("Failed to execute command:" + input);
            }


        }
    }
}
