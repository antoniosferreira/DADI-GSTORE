using System.Threading.Tasks;
using Grpc.Core;
using System;

namespace GSTORE_Client
{
    class StatusServices : StatusService.StatusServiceBase
    {

        private readonly Client Client;

        public StatusServices(in Client cl)
        {
            Client = cl;
        }

        public override Task<Stat> Status(Stat request, ServerCallContext context)
        {
            return Task.FromResult(ProcessStatus());
        }

        private Stat ProcessStatus()
        {
            Client.PrintStatus();
            return new Stat { };
        }

    }
}
