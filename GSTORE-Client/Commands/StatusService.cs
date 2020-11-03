using System.Threading.Tasks;
using Grpc.Core;

namespace GSTORE_Client
{
    class StatusService : ServerServices.ServerServicesBase
    {

        private readonly GSClient _client;

        public StatusService(in GSClient cl)
        {
            _client = cl;
        }

        public override Task<Empty> Status(Empty request, ServerCallContext context)
        {
            return Task.FromResult(ProcessStatus());
        }

        private Empty ProcessStatus()
        {
            _client.PrintStatus();
            return new Empty { };
        }

    }
}
