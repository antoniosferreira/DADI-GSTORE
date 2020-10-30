using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using GSTORE_Server.Storage;

namespace GSTORE_Server
{
    class ServerService : ServerServices.ServerServicesBase
    {

        private GSServer Server;

        public ServerService(in GSServer server) {
            Server = server;
        }

        // PARTITION OPERATION
        public override Task<Empty> Partition(PartitionRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessPartition(request)); ;
        }

        private Empty ProcessPartition(PartitionRequest request)
        {
            List<string> servers = new List<string>(request.Servers);
            Server.StorageServer.NewPartition(request.PartitionID, servers);
            return new Empty { };
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
            Server.StorageServer.PrintStatus();
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
            Server.StorageServer.Freeze();
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
            Server.StorageServer.Unfreeze();
            return new Empty { };
        }

    }
}
