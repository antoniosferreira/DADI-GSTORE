using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PM.Commands
{
    class Status : Command
    { 
        private readonly PM PuppetMaster;

        public Status(PM pm)
        {
            PuppetMaster = pm;

            Description = "Status";
            Rule = new Regex(
                @"Status",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            
            foreach (PMServices.PMServicesClient server in PuppetMaster.NodesCommunicator.GetAllServersClients())
            {
                void p()
                {
                    try
                    {
                        server.Status(new Empty { });
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to display status on some node");
                    }
                }

                Action action = p;
                Task task = new Task(action);
                task.Start();
            }


            foreach (StatusService.StatusServiceClient server in PuppetMaster.NodesCommunicator.GetAllClients())
            {
                void p()
                {
                    try
                    {
                        server.Status(new Stat { });
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to display status on some node");
                    }
                }

                Action action = p;
                Task task = new Task(action);
                task.Start();
            }
        }
    }
}
