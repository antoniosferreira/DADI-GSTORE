using System.Threading.Tasks;
using Grpc.Core;
using System;

namespace GSTORE_Server
{
    class ServerCommunicationService : ServerCommunicationServices.ServerCommunicationServicesBase
    {
        private readonly Server Server;
        
        public ServerCommunicationService(in Server server) {
            Server = server;
        }

        // Request to lock some object
        public override Task<Void> LockObject(LockObjectRequest request, ServerCallContext context)
        {
            try
            {
                Server.StorageServer.LockObject(request.PartitionID, request.ObjectID, request.WriteID);
                Console.WriteLine(">>> LockObject(" + request.PartitionID + " , " + request.ObjectID + " WriteID:" + request.WriteID + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> FAILED to LockObject(" + request.PartitionID + " , " + request.ObjectID + " WriteID:" + request.WriteID + ")");
                Console.WriteLine(e.StackTrace);
            }

            return Task.FromResult(new Void { });
        }


        // Request to update some object and unlock it afterwards
        public override Task<Void> WriteObject(WriteObjectRequest request, ServerCallContext context)
        {
            try
            {
                Server.StorageServer.WriteObject(request.PartitionID, request.ObjectID, request.Value, request.WriteID);
                Console.WriteLine(">>> WriteObject(" + request.PartitionID + " , " + request.ObjectID + " , " + request.Value + ")");
            }
            catch (Exception)
            {
                Console.WriteLine(">>> FAILED to WriteObject(" + request.PartitionID + " , " + request.ObjectID + " , " + request.Value + ")");
            }

            return Task.FromResult(new Void { });
        }
    }
}
