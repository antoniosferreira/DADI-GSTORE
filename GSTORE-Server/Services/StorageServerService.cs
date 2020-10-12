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
            Console.WriteLine("Read");

            string value = _server.Read(request.Key);
            
            return new ReadReply
            {
                Value = value
            };
        }


        // WRITE OPERATION
        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessWriteRequest(request));
        }

        private WriteReply ProcessWriteRequest(WriteRequest request)
        {
            Console.WriteLine("Write");

            bool success = _server.Write(request.Key, request.Value);

            return new WriteReply
            {
                Success = success
            };
        }

    }
}
