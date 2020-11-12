using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace GSTORE_Client.Commands
{
    class Read : Command
    {
        public Read(Client client)
        {
            Client = client;    

            Description = "Read <partitionID> <objectID> <serverID>";
            Rule = new Regex(
                @"Read (?<partitionID>\w+)\s+(?<objectID>\w+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            string[] sinput = input.Split(" ");
            Match match = Rule.Match(input);
            
            if (!match.Success)
            {
                Console.WriteLine(">>> FAILED to parse command Read");
                return;
            }

            string partitionID = match.Groups["partitionID"].Value;
            string objectID = match.Groups["objectID"].Value;
            string serverID = (sinput.Length == 4) ? sinput[3] : "-1";

            // Prepares list of servers to contact
            List<string> serversToContact = new List<string>();
            foreach (string sid in Client.NodesCommunicator.GetAllServersID())
                serversToContact.Add(sid);
            if (!(Client.CurrentServer == null)) 
            {
                string temp = Client.CurrentServer;
                serversToContact.Remove(Client.CurrentServer);
                serversToContact.Insert(0, temp);
            }
            if ((!serverID.Equals(Client.CurrentServer)) && (!serverID.Equals("-1")))
            {
                serversToContact.Remove(serverID);
                serversToContact.Insert(1, serverID);
            }

            // CREATES THE REQUEST
            ReadRequest readRequest = new ReadRequest
            {
                PartitionID = partitionID,
                ObjectID = objectID
            };

            int attempts = 0;

            do
            {
                Client.CurrentServer = serversToContact[attempts];
                
                try 
                {
                    ReadReply reply = Client.NodesCommunicator.GetServerClient(Client.CurrentServer).Read(readRequest);

                    if (reply.Success)
                    {
                        Console.WriteLine(">>> READ -> Key:" + objectID + " WITH Value: " + reply.Value);
                        return;
                    }
                    // Object not on partition
                    else if (reply.Value.Equals("N/A"))
                    {
                        Console.WriteLine(">>> " + reply.Value);
                        return;
                    }

                    if (attempts == (serversToContact.Count - 1))
                        break;

                    attempts += 1;
                }
                catch (Exception) {
                    Console.WriteLine(">>> Server {0} failed", Client.CurrentServer);
                    attempts += 1;
                    Client.NodesCommunicator.DeactivateServer(Client.CurrentServer);
                    Client.CurrentServer = null;
                }

            } while (true);


            Console.WriteLine(">>> N/A");
        }
    }
}
