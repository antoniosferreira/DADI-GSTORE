using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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


        // NEW LOCAL PARTITION
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
                ConsoleWrite(">>> New Partition " + request.PartitionID, ConsoleColor.DarkGreen);
            }
            catch (Exception)
            {
                ConsoleWrite(">>> Failed to process new partition " + request.PartitionID, ConsoleColor.DarkRed);
                Console.ReadKey();
                Environment.Exit(-1);
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
                ConsoleWrite(">>> Failed to print Status", ConsoleColor.DarkRed);
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
                ConsoleWrite(">>> Process is now being FREEZED", ConsoleColor.DarkCyan);
                Storage.Freeze();
            }
            catch (Exception)
            {
                ConsoleWrite(">>> Failed to FREEZE the process", ConsoleColor.DarkRed);
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
                ConsoleWrite(">>> Process is now UNFREEZED!", ConsoleColor.DarkCyan);
            }
            catch (Exception)
            {
                ConsoleWrite(">>> Failed to UNFREEZE the process", ConsoleColor.DarkRed);
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
