using System;
using System.Threading.Tasks;
using Grpc.Core;
using GSTORE_Server.Storage;


namespace GSTORE_Server
{
    class StorageServerService : StorageServerServices.StorageServerServicesBase
    {
        private readonly Server Server;
        public StorageServerService(in Server server) {
            Server = server;
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
                (success, value) = Server.StorageServer.Read(request.PartitionID, request.ObjectID);
                Console.WriteLine(">>> Read Processed: " + request.PartitionID + " : " + request.ObjectID);
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> Failed to Read: " + request.PartitionID + " : " + request.ObjectID);
                Console.WriteLine(e.StackTrace);
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
            string sid = "-1";

            try
            {
                (success, sid) = Server.StorageServer.Write(new WriteData(request));
                Console.WriteLine(">>> Processed write: " + request.PartitionID + " - " + request.ObjectID + " : " + request.Value + "| SUCCES:" + success);
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> Failed to process write: " + request.PartitionID + " - " + request.ObjectID + " : " + request.Value);
                Console.WriteLine(e.StackTrace);
            }

            return new WriteReply
            {
                Success = success,
                ServerID = sid
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
                reply.Listings.Add(Server.StorageServer.ListServerPartitions());
                Console.WriteLine(">>> Processed listServer ");
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> Failed to process listServer ");
                Console.WriteLine(e.StackTrace);
            }

            return reply;
        }
    }
}
