using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PM.Commands
{
    class Client : Command
    {
        private readonly PCSServices.PCSServicesClient PCSClient;

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
            Task.Run(() => {
                Match match = Rule.Match(input);

                if (!match.Success) {
                    Console.WriteLine(">>> FAILED to parse command Client");
                    return; 
                }

                string username = match.Groups["username"].Value;
                string clientURL = match.Groups["URL"].Value;
                string scriptFile = match.Groups["scriptFile"].Value;
                try
                {
                    var reply = PCSClient.InitClientAsync(
                        new ClientRequest
                        {
                            Username = username,
                            ClientUrl = clientURL,
                            ScriptFile = scriptFile,
                        }
                    );

                    Console.WriteLine(">>> Client {0} initiated", username);

                } catch (Exception) {
                    Console.WriteLine(">>> Failed to init client " + username);
                }
            });
        }
    }
}
