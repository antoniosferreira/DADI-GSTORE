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


        // PARTITION REPLICATION REQUEST
        public override Task<SuccessReply> SetPartition(SetPartitionRequest request, ServerCallContext context)
        {
            List<string> servers = new List<string>(request.AssociatedServers);

            Console.WriteLine(">>> SetPartition(" + request.PartitionID + " , " + request.MainServerID + ")");
            Server.StorageServer.InitPartition(request.PartitionID.ToUpper(), request.MainServerID, servers);
            
            return Task.FromResult(new SuccessReply());
        }


        // LOCK OBJECT
        public override Task<LockObjectReply> LockObject(LockObjectRequest request, ServerCallContext context)
        {
            Console.WriteLine(">>> LockObject(" + request.PartitionID + " , " + request.ObjectID + " WriteID:"  +request.WriteID + ")");
            Server.StorageServer.LockObject(request.PartitionID.ToUpper(), request.ObjectID, request.WriteID);
            Console.WriteLine(">>> FINISHED LockObject(" + request.PartitionID + " , " + request.ObjectID + " WriteID:" + request.WriteID + ")");

            return Task.FromResult(new LockObjectReply { });
        }


        // WRITE OBJECT
        public override Task<WriteObjectReply> WriteObject(WriteObjectRequest request, ServerCallContext context)
        {
            Console.WriteLine(">>> WriteObject(" + request.PartitionID + " , " + request.ObjectID + " , " + request.Value + ")");
            Server.StorageServer.WriteObject(request.PartitionID.ToUpper(), request.ObjectID, request.Value, request.WriteID);
            return Task.FromResult(new WriteObjectReply { });
        }

        

    }
}
