using System;
using System.Text.RegularExpressions;


namespace GSTORE_Client.Commands
{
    class ListServer : Command
    {
        public ListServer(GSClient client)
        {
            Client = client;

            Description = "listServer <serverID>";
            Rule = new Regex(
                @"listServer (?<serverID>\w+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Match match = Rule.Match(input);
            try
            {
                string serverID = match.Groups["serverID"].Value;

                try
                {
                    ListServerReply reply = Client.NodesCommunicator.GetServerClient(serverID).ListServer(new ListServerRequest { });
                    foreach (string s in reply.Listings)
                        Console.WriteLine(s);

                }
                catch (Exception e)
                {
                    Console.WriteLine(">>> Couldn't list server " + serverID);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> Bad Input");
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
