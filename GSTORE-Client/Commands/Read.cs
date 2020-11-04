using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace GSTORE_Client.Commands
{
    class Read : Command
    {
        public Read(GSClient client)
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
            try
            {
                int attempts = 0;
                int rounds = 0;

                string partitionID = match.Groups["partitionID"].Value;
                string objectID = match.Groups["objectID"].Value;
                string serverID = (sinput.Length == 4) ? sinput[3] : Client.NodesCommunicator.GetServerIDAtIndex(0);


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


                do
                {
                    if (rounds > 3)
                    {
                        Console.WriteLine(">>> Failed to perform read");
                        return;
                    }


                    Client.CurrentServer = serversToContact[attempts];

                    try
                    {
                        
                        ReadReply reply = Client.NodesCommunicator.GetServerClient(Client.CurrentServer).Read(readRequest);

                        if (reply.Success)
                        {
                            Console.WriteLine(">>> Key:" + objectID + " | Value: " + reply.Value);
                            return;
                        }
                        else
                        {
                            // Not in the partition
                            if (reply.Value.Equals("N/A"))
                            {
                                Console.WriteLine(">>> " + reply.Value);
                                return;

                                // Partition not in the server
                            }
                            else if (reply.Value.Equals("-1"))
                            {
                                attempts += 1;
                                if (attempts == (serversToContact.Count - 1)) {
                                    attempts += 1;
                                    rounds += 1;
                                }
                            }
                        }

                    }
                    catch (Exception) {
                        attempts += 1;
                        if (attempts == (serversToContact.Count - 1))
                        {
                            attempts += 1;
                            rounds += 1;
                        }
                    }

                    
                } while (true);
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> Failed to perform read");
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
