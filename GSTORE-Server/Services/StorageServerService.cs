using System;
using System.Threading.Tasks;
using Grpc.Core;
using GSTORE_Server.Storage;

namespace GSTORE_Server
{
    class StorageServerService : StorageServerServices.StorageServerServicesBase
    {

        private GSServer Server;

        public StorageServerService(in GSServer server) {
            Server = server;
        }


        // READ OPERATION
        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessReadRequest(request));
        }

        private ReadReply ProcessReadRequest(ReadRequest request)
        {
            bool success;
            string value;

            Console.WriteLine(">>> Processing read: " + request.PartitionID + " : " + request.ObjectID);

            (success, value) = Server.StorageServer.Read(request.PartitionID.ToUpper(), request.ObjectID);

            return new ReadReply
            {
                Value = value,
                Success = success
            };
        }


        // WRITE OPERATION
        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessWriteRequest(request));
        }

        private WriteReply ProcessWriteRequest(WriteRequest request)
        {
            bool success = false;
            string sid = "-1";

            Console.WriteLine(">>> Processing write: " + request.PartitionID + " - " + request.ObjectID + " : " + request.Value);
            (success, sid)= Server.StorageServer.Write(request.PartitionID.ToUpper(), request.ObjectID, request.Value);

            return new WriteReply
            {
                Success = success,
                ServerID = sid
            };
        }


        // LIST GLOBAL OPERATION
        public override Task<ListGlobalReply> ListGlobal(ListGlobalRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessListGlobal(request));
        }
        private ListGlobalReply ProcessListGlobal(ListGlobalRequest request)
        {
 
            Console.WriteLine(">>> Processing listGlobal ");

            return new ListGlobalReply
            {
                Listing = Server.StorageServer.ListMasterPartitions()
            };
        }


        // LIST SERVER OPERATION
        public override Task<ListServerReply> ListServer(ListServerRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessListServer(request));
        }
        private ListServerReply ProcessListServer(ListServerRequest request)
        {

            Console.WriteLine(">>> Processing listServer ");

            return new ListServerReply
            {
                Listing = Server.StorageServer.ListServerPartitions()
            };
        }
    }
}
