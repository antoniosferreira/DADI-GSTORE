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

            (success, value) = Server.StorageServer.Read(request.PartitionID, request.ObjectID);
            
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

            (success, sid)= Server.StorageServer.Write(request.PartitionID, request.ObjectID, request.Value);

            return new WriteReply
            {
                Success = success,
                ServerID = sid
            };
        }

    }
}
