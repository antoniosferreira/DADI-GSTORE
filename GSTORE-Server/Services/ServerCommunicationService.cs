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
                Server.StorageServer.LockObject(request.PartitionID, request.ObjectID);
                Console.WriteLine(">>> LockObject(" + request.PartitionID + " , " + request.ObjectID + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine(">>> FAILED to LockObject(" + request.PartitionID + " , " + request.ObjectID + ")");
                Console.WriteLine(e.StackTrace);
            }

            return Task.FromResult(new Void { });
        }


        // Request to update some object and unlock it afterwards
        public override Task<Void> WriteObject(WriteObjectRequest request, ServerCallContext context)
        {
            try
            {
                Server.StorageServer.WriteObject(request.PartitionID, request.ObjectID, request.Value, request.TID);
                Console.WriteLine(">>> WriteObject(" + request.PartitionID + " , " + request.ObjectID + " , " + request.Value + ")");
            }
            catch (Exception)
            {
                Console.WriteLine(">>> FAILED to WriteObject(" + request.PartitionID + " , " + request.ObjectID + " , " + request.Value + ")");
            }

            return Task.FromResult(new Void { });
        }


        // SEQUENCER
        public override Task<TIDReply> GetTID(TIDRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessTID(request)); ;
        }
        private TIDReply ProcessTID(TIDRequest request)
        {
            int value = -1;
            try
            {
                value = Server.StorageServer.GetNewTID(request.PartitionID, request.ObjectID);
                Console.WriteLine(">>> Processed GetNewTid({0},{1}) with {2}", request.PartitionID, request.ObjectID, value);
            }
            catch (Exception e)
            {
                Console.WriteLine(">>>>>>> FAILED to ProcessTID(" + request.PartitionID + request.ObjectID + ")");
                Console.WriteLine(e.StackTrace);
            }

            return new TIDReply { TID = value };
        }


        public override Task<Void> ElectLeader(LeaderElectionRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessElection(request)); ;
        }
        private Void ProcessElection(LeaderElectionRequest request)
        {
            Server.StorageServer.ProcessElection(request.Pid, request.Sid);
            return new Void { };
        }

        public override Task<Void> ConfirmLeader(LeaderConfirmationRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessLeaderConfirmation(request)); ;
        }
        private Void ProcessLeaderConfirmation(LeaderConfirmationRequest request)
        {
            Server.StorageServer.ProcessLeaderConfirmation(request.Pid, request.Sid);
            return new Void { };
        }

    }
}
