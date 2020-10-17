using System;
using System.Threading.Tasks;
using Grpc.Core;
using GSTORE_Server.Storage;

namespace GSTORE_Server
{
    class ServerCommunicationService : ServerCommunicationServices.ServerCommunicationServicesBase
    {

        private StorageServer Storage;
        
        public ServerCommunicationService(StorageServer storage) {
            Storage = storage;
        }


        // LOCK OBJECT
        public override Task<LockObjectReply> LockObject(LockObjectRequest request, ServerCallContext context)
        {
            Storage.LockObject(request.PartitionID, request.ObjectID);   
            return Task.FromResult(new LockObjectReply { });
        }

        // WRITE OBJECT
        public override Task<WriteObjectReply> WriteObject(WriteObjectRequest request, ServerCallContext context)
        {
            Storage.WriteObject(request.PartitionID, request.ObjectID, request.Value);
            return Task.FromResult(new WriteObjectReply { });
        }

        

    }
}
