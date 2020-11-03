using System;
using System.Text.RegularExpressions;

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
                bool init = true;
                int attempts = 0;
                int rounds = 0;

                string partitionID = match.Groups["partitionID"].Value;
                string objectID = match.Groups["objectID"].Value;
                string serverID = (sinput.Length == 4) ? sinput[3] : Client.NodesCommunicator.GetServerIDAtIndex(0);

                if (serverID.Equals("-1")) { 
                    serverID = Client.NodesCommunicator.GetServerIDAtIndex(0);
                }

                if (Client.CurrentServer == null)
                {
                    Client.CurrentServer = serverID;
                    init = false;
                }

                ReadRequest readRequest = new ReadRequest
                {
                    PartitionID = partitionID,
                    ObjectID = objectID
                };


                do
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
                        } else if (reply.Value.Equals("-1")) {
                            if (init)
                            {
                                Client.CurrentServer = serverID;
                                init = false;
                            }
                            else
                            {
                                Client.CurrentServer = Client.NodesCommunicator.GetServerIDAtIndex(attempts);

                                attempts += 1;
                                if (attempts == Client.NodesCommunicator.GetServersCounter() - 1) attempts = 0;
                                
                                if (attempts == 0)
                                {
                                    rounds += 1;
                                    if (rounds == 3)
                                    {
                                        Console.WriteLine(">>> Failed to read");
                                        return;
                                    }
                                }
                            }
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
