using System.Threading.Tasks;
using Grpc.Core;

namespace GSTORE_Client
{
    class StatusService : ServerServices.ServerServicesBase
    {

        private readonly Client Client;

        public StatusService(in Client cl)
        {
            Client = cl;
        }

        public override Task<Empty> Status(Empty request, ServerCallContext context)
        {
            return Task.FromResult(ProcessStatus());
        }

        private Empty ProcessStatus()
        {
            Client.PrintStatus();
            return new Empty { };
        }

    }
}
