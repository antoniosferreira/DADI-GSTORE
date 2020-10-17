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
                string partitionID = match.Groups["partitionID"].Value;
                string objectID = match.Groups["objectID"].Value;
                string serverID = match.Groups["serverID"].Value;

                if (Client.CurrentServer == null)
                    Client.CurrentServer = serverID;

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
                        Console.WriteLine("Read " + partitionID + objectID + ": " + reply.Value);
                        return;
                    }
                    else
                    {
                        if (Client.CurrentServer.Equals(serverID))
                        {
                            Console.WriteLine("N/A");
                            return;
                        }

                        Client.CurrentServer = serverID;
                    }

                } while (true);


            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
