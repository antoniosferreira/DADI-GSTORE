using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using GSTORE_Client.Communication;

namespace GSTORE_Client.Commands
{
    class ListGlobal : Command
    {
        public ListGlobal(Client client)
        {
            Client = client;

            Description = "listGlobal";
            Rule = new Regex(
               @"listGlobal",
               RegexOptions.IgnoreCase | RegexOptions.Compiled);

        }

        public override void Exec(string input)
        {
            List<StorageServerServices.StorageServerServicesClient> serversList = Client.NodesCommunicator.GetAllServers();
            List<Task> requestTasks = new List<Task>();
            List<string> listings = new List<string>();

            foreach (StorageServerServices.StorageServerServicesClient server in serversList)
            {
                void requestServer()
                {
                    try
                    {
                        ListServerReply reply = server.ListServer(new ListServerRequest { });
                        listings.Add("--------------- SERVER---------------\n");
                        foreach (string l in reply.Listings)
                        {
                            if (!listings.Contains(l))
                                listings.Add(l);
                        }
                    } catch (Exception)
                    {
                        Client.NodesCommunicator.DeactivateServer(Client.CurrentServer);
                    }
                }
                
                Task task = new Task(requestServer);
                requestTasks.Add(task);
                task.Start();

                Task.WaitAll(requestTasks.ToArray());

                // Displays the result
                foreach (string l in listings.Distinct())
                    Console.Write(l);
            }
        }
    }
}
