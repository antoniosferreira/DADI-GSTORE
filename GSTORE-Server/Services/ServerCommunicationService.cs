using System.Threading.Tasks;
using Grpc.Core;
using System;
using GSTORE_Server.Storage;

namespace GSTORE_Server
{
    class ServerCommunicationService : ServerCommunicationServices.ServerCommunicationServicesBase
    {
        private readonly Server Server;
        
        public ServerCommunicationService(in Server server) {
            Server = server;
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



        public override Task<Void> HeartBeat(Void request, ServerCallContext context)
        {
            return Task.FromResult(new Void { });
        }



        public override Task<WriteResult> StartWrite(WriteRequestData request, ServerCallContext context)
        {
            return Task.FromResult(ProcessStartWrite(request)); ;
        }
        private WriteResult ProcessStartWrite(WriteRequestData request)
        {
            WriteData write = new WriteData(request.Tid, request.Pid, request.Oid, request.Value);
            return new WriteResult { Success = Server.StorageServer.StartWrite(write) }; 
        }




        public override Task<Void> DeliverPrepareRequest(WriteRequestData request, ServerCallContext context)
        {
            return Task.FromResult(ProcessPrepareRequest(request)); ;
        }

        private Void ProcessPrepareRequest(WriteRequestData request)
        {
            WriteData write = new WriteData(request.Tid, request.Pid, request.Oid, request.Value);
            Server.StorageServer.DeliverPrepareRequest(write);
            return new Void {};
        }




        public override Task<WriteRequestData> RetrieveWrite(WriteRetrievalRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessRetrieveWrite(request)); ;
        }

        private WriteRequestData ProcessRetrieveWrite(WriteRetrievalRequest request)
        {
            WriteData data = Server.StorageServer.RetrieveWrite(request.Tid, request.Pid);
            WriteRequestData write = new WriteRequestData { Oid =data.Oid, Pid =data.Pid, Tid =data.Tid, Value =data.Value};
            return write;
        }
    }
}
