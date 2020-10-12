using System;
using System.Threading.Tasks;
using Grpc.Core;
using GSTORE_Server.Storage;

namespace GSTORE_Server
{
    class ServerService : ServerServices.ServerServicesBase
    {

        private StorageServer _server;

        public ServerService(StorageServer server) {
            _server = server;
        }


        // CRASH OPERATION
        public override Task<Empty> Crash(Empty request, ServerCallContext context)
        {
            Console.WriteLine("CRASHED");
            Environment.Exit(1);
            return null;
        }

        // STATUS OPERATION
        public override Task<Empty> Status(Empty request, ServerCallContext context)
        {
            return Task.FromResult(ProcessStatus());
        }

        private Empty ProcessStatus()
        {
            _server.PrintStatus();
            return new Empty { };
        }
        // FREEZE OPERATION
        public override Task<Empty> Freeze(Empty request, ServerCallContext context)
        {
            return Task.FromResult(ProcessFreeze());
        }

        private Empty ProcessFreeze()
        {
            Console.WriteLine("ProcessFreeze");
            _server.Freeze();
            return new Empty { } ;
        }

        // UNFREEZE OPERATION
        public override Task<Empty> Unfreeze(Empty request, ServerCallContext context)
        {
            return Task.FromResult(ProcessUnfreeze());
        }

        private Empty ProcessUnfreeze()
        {
            Console.WriteLine("ProcessUnfreeze");
            _server.Unfreeze();
            return new Empty { };
        }

    }
}
