using System;
using System.Threading.Tasks;
using Grpc.Core;
using GSTORE_Server.Storage;

namespace GSTORE_Server
{
    class StorageServerService : StorageServerServices.StorageServerServicesBase
    {

        private StorageServer _server;

        public StorageServerService(StorageServer server) {
            _server = server;
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

            (success, value) = _server.Read(request.PartitionID, request.ObjectID);
            
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
            
            (success, sid)= _server.Write(request.PartitionID, request.ObjectID, request.Value);

            return new WriteReply
            {
                Success = success,
                ServerID = sid
            };
        }

    }
}
