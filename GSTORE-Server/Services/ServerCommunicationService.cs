using System.Threading.Tasks;
using Grpc.Core;
using GSTORE_Server.Storage;
using System.Collections.Generic;
using System;

namespace GSTORE_Server
{
    class ServerCommunicationService : ServerCommunicationServices.ServerCommunicationServicesBase
    {

        private readonly GSServer Server;
        
        public ServerCommunicationService(in GSServer server) {
            Server = server;
        }


        // Request to create a replicate of existing partition
        public override Task<SuccessReply> SetPartition(SetPartitionRequest request, ServerCallContext context)
        {
            try
            {
                List<string> servers = new List<string>(request.AssociatedServers);
                Server.StorageServer.InitPartition(request.PartitionID.ToUpper(), request.MainServerID, servers);
                Console.WriteLine(">>> SetPartition(" + request.PartitionID + " , " + request.MainServerID + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> FAILED to SetPartition(" + request.PartitionID + " , " + request.MainServerID + ")");
                Console.WriteLine(e.StackTrace);
            }

            return Task.FromResult(new SuccessReply());
        }


        // Request to lock some object
        public override Task<LockObjectReply> LockObject(LockObjectRequest request, ServerCallContext context)
        {
            try
            {
                Server.StorageServer.LockObject(request.PartitionID.ToUpper(), request.ObjectID, request.WriteID);
                Console.WriteLine(">>> LockObject(" + request.PartitionID + " , " + request.ObjectID + " WriteID:" + request.WriteID + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> FAILED to LockObject(" + request.PartitionID + " , " + request.ObjectID + " WriteID:" + request.WriteID + ")");
                Console.WriteLine(e.StackTrace);
            }

            return Task.FromResult(new LockObjectReply { });
        }


        // Request to update some object and unlock it afterwards
        public override Task<WriteObjectReply> WriteObject(WriteObjectRequest request, ServerCallContext context)
        {
            try
            {
                Server.StorageServer.WriteObject(request.PartitionID.ToUpper(), request.ObjectID, request.Value, request.WriteID);
                Console.WriteLine(">>> WriteObject(" + request.PartitionID + " , " + request.ObjectID + " , " + request.Value + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> FAILED to WriteObject(" + request.PartitionID + " , " + request.ObjectID + " , " + request.Value + ")");
            }

            return Task.FromResult(new WriteObjectReply { });
        }
    }
}
