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
                @"Read (?<partitionID>\w+)\s+(?<objectID>\w+)\s+(?<serverID>\w+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {

            Match match = Rule.Match(input);
            try
            {
                bool init = true;
                int attempts = 0;

                string partitionID = match.Groups["partitionID"].Value;
                string objectID = match.Groups["objectID"].Value;
                string serverID = match.Groups["serverID"].Value;

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
                                Client.CurrentServer = Client.NodesCommunicator.GetServerIDAtIndex(attempts);
                            
                            attempts+=1;
                        }
                    }
                } while (true);
            }
            catch (Exception)
            {
                Console.WriteLine(">>> Failed to perform read");
            }
        }
    }
}
