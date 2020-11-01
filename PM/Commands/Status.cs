using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks;


namespace PM.Commands
{
    class Status : Command
    { 
        private PM PuppetMaster;

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
                List<ServerServices.ServerServicesClient> serversList = PuppetMaster.NodesCommunicator.GetAllServersClients();
                List<ServerServices.ServerServicesClient> clientsList = PuppetMaster.NodesCommunicator.GetAllClients();

                foreach (ServerServices.ServerServicesClient server in serversList)
                    Task.Run(() => server.Status(new Empty { }));

                foreach (ServerServices.ServerServicesClient client in clientsList)
                    Task.Run(() => client.Status(new Empty { }));

            }
            catch (Grpc.Core.RpcException e)
            {
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

    }
}
