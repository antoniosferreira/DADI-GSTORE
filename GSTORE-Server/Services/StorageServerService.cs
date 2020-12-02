using System;
using System.Threading.Tasks;

using GSTORE_Server.Storage;

using Grpc.Core;


namespace GSTORE_Server
{
    class StorageServerService : StorageServerServices.StorageServerServicesBase
    {
        private readonly StorageServer Storage;
        public StorageServerService(in StorageServer storage) {
            Storage = storage;
        }


        // Client's Access to Read Objects
        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessReadRequest(request));
        }
        private ReadReply ProcessReadRequest(ReadRequest request)
        {
            bool success = false;
            string value = "-1";

            try
            {
                (success, value) = Storage.Read(request.PartitionID, request.ObjectID);
                ConsoleWrite("StorageServerService: Read processed :" + request.PartitionID + request.ObjectID, ConsoleColor.DarkGreen);
            }
            catch (Exception )
            {
                ConsoleWrite("StorageServerService: Read failed :" + request.PartitionID + request.ObjectID, ConsoleColor.DarkRed);
            }


            return new ReadReply
            {
                Value = value,
                Success = success
            };
        }


        // Client's Access to Write Objects
        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessWriteRequest(request));
        }
        private WriteReply ProcessWriteRequest(WriteRequest request)
        {
            bool success = false;
            try
            {
                success = Storage.Write(new WriteData(request));
                ConsoleWrite("StorageServerService: Write processed :" + request.PartitionID + request.ObjectID + request.Value, ConsoleColor.DarkGreen);
            }
            catch (Exception )
            {
                ConsoleWrite("StorageServerService: Write failed :" + request.PartitionID + request.ObjectID + request.Value, ConsoleColor.DarkRed);
            }

            return new WriteReply
            {
                Success = success,
            };
        }


        // Clients access to all stored data
        public override Task<ListServerReply> ListServer(ListServerRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessListServer());
        }
        private ListServerReply ProcessListServer()
        {
            ListServerReply reply = new ListServerReply { };

            try
            {
                reply.Listings.Add(Storage.ListServerPartitions());
                ConsoleWrite("StorageServerService: ListServer processed", ConsoleColor.DarkGreen);
            }
            catch (Exception)
            {
                ConsoleWrite("StorageServerService: ListServer failed", ConsoleColor.DarkRed);
            }

            return reply;
        }



        private void ConsoleWrite(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
