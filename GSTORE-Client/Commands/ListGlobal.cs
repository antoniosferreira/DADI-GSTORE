using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GSTORE_Client.Commands
{
    class ListGlobal : Command
    {
        public ListGlobal(GSClient client)
        {
            Client = client;

            Description = "listGlobal";
            Rule = new Regex(
                @"listGlobal .*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {

            List<StorageServerServices.StorageServerServicesClient> serversList = Client.NodesCommunicator.GetAllServers();
            foreach (StorageServerServices.StorageServerServicesClient server in serversList)
            {
                Task.Run(() => {
                    try
                    {
                        ListServerReply reply = server.ListServer(new ListServerRequest { });
                        Console.WriteLine(reply.Listing);
                    } catch (Exception)
                    {
                        Console.WriteLine("----- Failed to list server -----");
                    }

                });
            }


        }
    }
}
