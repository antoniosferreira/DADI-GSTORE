using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;

namespace GSTORE_Server
{
    class PMService : PMServices.PMServicesBase
    {
        private readonly Server Server;

        public PMService(in Server server) {
            Server = server;
        }


        // Creates a new Partition
        public override Task<Empty> Partition(PartitionRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessPartition(request)); ;
        }
        private Empty ProcessPartition(PartitionRequest request)
        {
            try
            {
                List<string> servers = new List<string>(request.Servers);
                Server.StorageServer.NewPartition(request.PartitionID, servers);
                Console.WriteLine(">>> ProcessPartition(" + request.PartitionID + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> FAILED to ProcessPartition(" + request.PartitionID + ")");
                Console.WriteLine(e.StackTrace);
            }

            return new Empty { };
        }


        // CRASH OPERATION
        public override Task<Empty> Crash(Empty request, ServerCallContext context)
        {
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
            try
            {
                Server.StorageServer.PrintStatus();
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> Failed to Print Status");
                Console.WriteLine(e.StackTrace);
            }

            return new Empty { };
        }


        // FREEZE OPERATION
        public override Task<Empty> Freeze(Empty request, ServerCallContext context)
        {
            return Task.FromResult(ProcessFreeze());
        }

        private Empty ProcessFreeze()
        {
            try
            {
                Server.StorageServer.Freeze();
                Console.WriteLine(">>> Process is now FREEZED!");
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> Failed to FREEZE the process");
                Console.WriteLine(e.StackTrace);
            }


            return new Empty { } ;
        }


        // UNFREEZE OPERATION
        public override Task<Empty> Unfreeze(Empty request, ServerCallContext context)
        {
            return Task.FromResult(ProcessUnfreeze());
        }

        private Empty ProcessUnfreeze()
        {
            try
            {
                Server.StorageServer.Unfreeze();
                Console.WriteLine(">>> Process is now UNFREEZED!");
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> Failed to UNFREEZE the process");
                Console.WriteLine(e.StackTrace);
            }


            return new Empty { };
        }
    }
}
