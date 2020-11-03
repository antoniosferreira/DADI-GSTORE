using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Reflection.Metadata.Ecma335;

namespace GSTORE_Client.Commands
{
    class ListGlobal : Command
    {
        public ListGlobal(GSClient client)
        {
            Client = client;

            Description = "listGlobal";
            Rule = new Regex(
               @"listGlobal",
               RegexOptions.IgnoreCase | RegexOptions.Compiled);

        }

        public override void Exec(string input)
        {
            try
            {
                List<StorageServerServices.StorageServerServicesClient> serversList = Client.NodesCommunicator.GetAllServers();
                List<Task> requestTasks = new List<Task>();
                List<string> listings = new List<string>();

                foreach (StorageServerServices.StorageServerServicesClient server in serversList)
                {
                    Action action = () => {
                        ListServerReply reply = server.ListServer(new ListServerRequest { });
                        foreach (string l in reply.Listings)
                        {
                            if (!listings.Contains(l))
                                listings.Add(l); 
                        }
                    };
                    Task task = new Task(action);
                    requestTasks.Add(task);
                    task.Start();

                    Task.WaitAll(requestTasks.ToArray());


                    // Displays the result
                    foreach (string l in listings.Distinct())
                        Console.Write(l);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> Failed to execute command: " + input);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
