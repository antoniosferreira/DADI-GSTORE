using System;
using System.Threading.Tasks;

using System.Collections.Generic;

using Grpc.Core;

using GSTORE_Server.Storage;

namespace GSTORE_Server
{
    class PMService : PMServices.PMServicesBase
    {
        private readonly StorageServer Storage;

        public PMService(in StorageServer storage) {
            Storage = storage;
        }

        public override Task<Empty> Partition(PartitionRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessPartition(request)); ;
        }
        private Empty ProcessPartition(PartitionRequest request)
        {
            try
            {
                List<string> servers = new List<string>(request.Servers);

                Storage.NewPartition(request.PartitionID, servers);
                ConsoleWrite("PMService: New Partition " + request.PartitionID, ConsoleColor.DarkGreen);
            }
            catch (Exception)
            {
                ConsoleWrite("PMService: Failed New Partition " + request.PartitionID, ConsoleColor.DarkRed);
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
                Storage.PrintStatus();
            }
            catch (Exception)
            {
                ConsoleWrite("PMService: Failed to print status", ConsoleColor.DarkRed);
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
                ConsoleWrite("PMService: PROCESS FREEZED", ConsoleColor.Yellow);
                Storage.Freeze();
            }
            catch (Exception)
            {
                ConsoleWrite("PMService: Failed to process freeze", ConsoleColor.DarkRed);
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
                Storage.Unfreeze();
                ConsoleWrite("PMService: PROCESS UNFREEZED", ConsoleColor.Yellow);
            }
            catch (Exception)
            {
                ConsoleWrite("PMService: Failed to process unfreeze", ConsoleColor.DarkRed);
            }


            return new Empty { };
        }


        private void ConsoleWrite(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
