using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PM.Commands
{
    class Client : Command
    {
        private PCSServices.PCSServicesClient PCSClient;

        public Client(PCSServices.PCSServicesClient client)
        {
            PCSClient = client;

            Description = "Client <username> <clientURL> <scriptFile>";
            Rule = new Regex(
                @"Client (?<username>\w+)\s+(?<URL>[^ ]+)\s+(?<scriptFile>[^ ]+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Match match = Rule.Match(input);
            try {
                string username = match.Groups["username"].Value;
                string clientURL = match.Groups["URL"].Value;
                string scriptFile = match.Groups["scriptFile"].Value;

                var reply = PCSClient.InitClientAsync(
                         new ClientRequest {
                            Username = username,
                            ClientUrl = clientURL,
                            ScriptFile = scriptFile,
                         });

                return;

            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("Failed to execute command:" + input);
            }
        }


    }
}
