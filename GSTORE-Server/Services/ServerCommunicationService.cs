using System.Threading.Tasks;
using Grpc.Core;
using GSTORE_Server.Storage;
using System.Collections.Generic;
using System;

namespace GSTORE_Server
{
    class ServerCommunicationService : ServerCommunicationServices.ServerCommunicationServicesBase
    {

        private GSServer Server;
        
        public ServerCommunicationService(in GSServer server) {
            Server = server;
        }


        // PARTITION REQUEST
        public override Task<SuccessReply> SetPartition(SetPartitionRequest request, ServerCallContext context)
        {
            List<string> servers = new List<string>(request.AssociatedServers);
            Server.StorageServer.InitPartition(request.PartitionID, request.MainServerID, servers);
            return Task.FromResult(new SuccessReply());
        }



        // LOCK OBJECT
        public override Task<LockObjectReply> LockObject(LockObjectRequest request, ServerCallContext context)
        {
            Server.StorageServer.LockObject(request.PartitionID, request.ObjectID); 
            return Task.FromResult(new LockObjectReply { });
        }

        // WRITE OBJECT
        public override Task<WriteObjectReply> WriteObject(WriteObjectRequest request, ServerCallContext context)
        {
            Server.StorageServer.WriteObject(request.PartitionID, request.ObjectID, request.Value);
            return Task.FromResult(new WriteObjectReply { });
        }

        

    }
}
