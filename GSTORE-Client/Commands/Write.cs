using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;


namespace GSTORE_Client.Commands
{
    class Write : Command
    {
        public Write(GSClient client)
        {
            Client = client;

            Description = "Write <partitionID> <objpectID> <value>";
            Rule = new Regex(
                @"Write (?<partitionID>\w+)\s+(?<objectID>\w+)\s+(?<value>\w+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Match match = Rule.Match(input);
            try
            {
                int attempts = 0;
                bool writeSuccess = false;

                string partitionID = match.Groups["partitionID"].Value;
                string objectID = match.Groups["objectID"].Value;
                string value = match.Groups["value"].Value;


                // Creates Write Request
                WriteRequest writeRequest = new WriteRequest
                {
                    PartitionID = partitionID,
                    ObjectID = objectID,
                    Value = value
                };

                if (Client.CurrentServer == null)
                {
                    Client.CurrentServer = Client.NodesCommunicator.GetServerIDAtIndex(attempts);
                    attempts += 1;
                }

                do
                {
                    // Sends the Write to the Current Attached Server
                    writeSuccess = SendWriteRequest(writeRequest, Client.NodesCommunicator.GetServerClient(Client.CurrentServer));

                    // If failed to write, attempts to contact the next server
                    if (!writeSuccess)
                    {
                        Client.CurrentServer = Client.NodesCommunicator.GetServerIDAtIndex(attempts);
                        attempts += 1;
                    }

                } while (!writeSuccess);

            } catch (Exception e)
            {
                Console.WriteLine("Write failed");
            }

        }

        private bool SendWriteRequest(WriteRequest request, StorageServerServices.StorageServerServicesClient server)
        {
            WriteReply reply = server.Write(request);

            if (reply.Success)
            {
                Console.WriteLine(request.Value + " written into partition " + request.PartitionID + request.ObjectID);
                return true;
            } 

            // If received from server the master ID
            if (!reply.ServerID.Equals("-1"))
            {
                Client.CurrentServer = reply.ServerID;
                return SendWriteRequest(request, Client.NodesCommunicator.GetServerClient(Client.CurrentServer));
            }

            return false;
        }

    }
}
