using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace GSTORE_Client.Commands
{
    class Write : Command
    {
        public Write(Client client)
        {
            Client = client;

            Description = "Write <partitionID> <objectID> <value>";
            Rule = new Regex(
                @"Write (?<partitionID>\w+)\s+(?<objectID>[-_\w]+)\s+""(?<value>.*)"".*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Match match = Rule.Match(input);
            
            if (!match.Success)
            {
                Console.WriteLine(">>> FAILED to parse Write command");
                return;
            }

            string partitionID = match.Groups["partitionID"].Value;
            string objectID = match.Groups["objectID"].Value;
            string value = match.Groups["value"].Value;

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
            
            // Creates Write Request
            WriteRequest writeRequest = new WriteRequest
            {
                PartitionID = partitionID,
                ObjectID = objectID,
                Value = value
            };

            int attempts = -1;
            bool writeSuccess;
            do
            {
                if (attempts == (serversToContact.Count - 1))
                    break;

                attempts += 1;

                Client.CurrentServer = serversToContact[attempts];

                // Sends the Write to the Current Attached Server
                writeSuccess = SendWriteRequest(writeRequest, Client.NodesCommunicator.GetServerClient(Client.CurrentServer));

                if (writeSuccess) return;

            } while (!writeSuccess);

            Console.WriteLine(">>> Failed to write {0} on {1} {2}", value, partitionID, objectID);
        }

        private bool SendWriteRequest(WriteRequest request, StorageServerServices.StorageServerServicesClient server)
        {
            try
            {
                WriteReply reply = server.Write(request);
                if (reply.Success)
                {
                    Console.WriteLine(">>> Wrote {0} into {1} {2}", request.Value, request.PartitionID, request.ObjectID);
                    return true;
                }

                // If received from server the master ID
                if (!reply.ServerID.Equals("-1"))
                {
                    Client.CurrentServer = reply.ServerID;
                    return SendWriteRequest(request, Client.NodesCommunicator.GetServerClient(Client.CurrentServer));
                }

            } catch (Exception)
            {
                Console.WriteLine(">>> Server {0} failed", Client.CurrentServer);
                Client.NodesCommunicator.DeactivateServer(Client.CurrentServer);
                Client.CurrentServer = null;
            }

            return false;
        }

    }
}
