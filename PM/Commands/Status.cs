using System;
using System.Collections.Generic;
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
            try
            {
                List<PMServices.PMServicesClient> serversList = PuppetMaster.NodesCommunicator.GetAllServersClients();
                List<PMServices.PMServicesClient> clientsList = PuppetMaster.NodesCommunicator.GetAllClients();

                foreach (PMServices.PMServicesClient server in serversList)
                    Task.Run(() => server.Status(new Empty { }));

                foreach (PMServices.PMServicesClient client in clientsList)
                    Task.Run(() => client.Status(new Empty { }));

            }
            catch (Grpc.Core.RpcException)
            {
                Console.WriteLine(">>> STATUS: Some node was unreacheable");
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

    }
}
