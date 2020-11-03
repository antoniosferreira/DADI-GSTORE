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
            List<StorageServerServices.StorageServerServicesClient> serversList = Client.NodesCommunicator.GetAllServers();
            ConcurrentBag<string> listings = new ConcurrentBag<string>();
            Semaphore semaphore = new Semaphore(1, 1);

            List<Task> requestTasks = new List<Task>();
            foreach (StorageServerServices.StorageServerServicesClient server in serversList)
            {
                Action action = () => {
                    try
                    {
                        ListServerReply reply = server.ListServer(new ListServerRequest { });
                        foreach (string l in reply.Listings)
                        {
                            semaphore.WaitOne();
                            if (!listings.Contains(l))
                                listings.Add(l);
                            semaphore.Release();
                        }
                    } catch (Exception) 
                    {
                        ;
                    }
                };

                Task task = new Task(action);
                requestTasks.Add(task);
                task.Start();
            }

            Task.WaitAll(requestTasks.ToArray());

            foreach (string l in listings)
                Console.WriteLine(l);
        }
    }
}
