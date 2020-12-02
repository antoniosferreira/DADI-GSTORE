using System.Threading.Tasks;
using Grpc.Core;
using System;
using GSTORE_Server.Storage;
using System.Collections.Generic;

namespace GSTORE_Server
{
    class ServerCommunicationService : ServerCommunicationServices.ServerCommunicationServicesBase
    {
        private readonly StorageServer Storage;
        
        public ServerCommunicationService(in StorageServer storage) {
            Storage = storage;
        }


        public override Task<WriteResult> LaunchWrite(WriteRequestData request, ServerCallContext context)
        {
            return Task.FromResult(ProcessLaunchWrite(request)); ;
        }
        private WriteResult ProcessLaunchWrite(WriteRequestData request)
        {
            bool success = false;
            try
            {
                WriteData write = new WriteData(request.Tid, request.Pid, request.Oid, request.Value);
                success = Storage.LaunchWrite(write);

                string displaymessage = "ServerCommunicationService: Write processed "+ request.Pid + " - " + request.Oid + " - " + request.Value;
                ConsoleWrite(displaymessage, ConsoleColor.DarkGreen);
            }
            catch (Exception)
            {
                string displaymessage = "ServerCommunicationService: Failed to process " + request.Pid + " - " + request.Oid + " - " + request.Value;
                ConsoleWrite(displaymessage, ConsoleColor.DarkRed);
            }


            return new WriteResult { Success = success};
        }

        public override Task<Void> ViewDeliver(ViewDeliverRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessViewDelivery(request)); ;
        }
        private Void ProcessViewDelivery(ViewDeliverRequest request)
        {
            try
            {
                Storage.ProcessViewDelivery(request.ViewId, request.ViewLeader, new WriteData(request.Message));
                ConsoleWrite(">>> Received view delivery " + request.Message.Tid + " :" + request.Message.Pid + request.Message.Oid + request.Message.Value + " from view " + request.ViewId, ConsoleColor.DarkGreen);
            }
            catch (Exception)
            {
                ConsoleWrite(">>> Failed to process view delivery " + request.ViewId, ConsoleColor.DarkRed);
            }

            return new Void { };
        }

        public override Task<Void> ViewChange(ViewChangeRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessViewChange(request)); ;
        }

        private Void ProcessViewChange(ViewChangeRequest request)
        {
            try
            {
                // Parses the request
                List<string> participants = new List<String>();
                foreach (string server in request.ViewParticipants)
                    participants.Add(server);


                Storage.ProcessViewChange(request.Pid, request.ViewId, request.ViewLeader, participants, request.ViewSequencer);
                ConsoleWrite(">>> Updated new view" + request.ViewId + " from leader " + request.ViewLeader, ConsoleColor.DarkGreen);

            }
            catch (Exception)
            {
                ConsoleWrite(">>> Failed to process view change"+request.ViewId, ConsoleColor.DarkRed);
            }
            
            
            return new Void { };
        }

        public override Task<WriteRequestData> RetrieveWrite(WriteRetrievalRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessRetrieveWrite(request)); ;
        }

        private WriteRequestData ProcessRetrieveWrite(WriteRetrievalRequest request)
        {
            try
            {
                WriteData data = Storage.RetrieveWrite(request.Pid, request.Tid, request.ViewId);
                WriteRequestData write = new WriteRequestData { Oid = data.Oid, Pid = data.Pid, Tid = data.Tid, Value = data.Value };

                if (data != null)
                {
                    ConsoleWrite(">>> Retrieved write" + request.Tid + " from view " + request.ViewId, ConsoleColor.DarkGreen);
                    return write;
                }

            } catch (Exception)
            { 
            }

            ConsoleWrite(">>> Failed to Retrieve write " + request.Tid + " on view " + request.ViewId, ConsoleColor.DarkRed);
            return null;
        }







        public override Task<Void> ElectLeader(LeaderElectionRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessElection(request)); ;
        }
        private Void ProcessElection(LeaderElectionRequest request)
        {
            try
            {
                Storage.ProcessElection(request.Pid, request.Sid);
                ConsoleWrite(">>> Processed election for partition " + request.Pid, ConsoleColor.DarkGreen);

            }
            catch (Exception)
            {
                ConsoleWrite(">>> Failed to process election", ConsoleColor.DarkRed);
            }

            return new Void { };
        }



        public override Task<Void> ConfirmLeader(ViewChangeRequest request, ServerCallContext context)
        {
            return Task.FromResult(ProcessLeaderConfirmation(request)); ;
        }
        private Void ProcessLeaderConfirmation(ViewChangeRequest request)
        {
            try
            {
                // Parses the request
                List<string> participants = new List<String>();
                foreach (string server in request.ViewParticipants)
                    participants.Add(server);

                Storage.ProcessViewChange(request.Pid, request.ViewId, request.ViewLeader, participants, request.ViewSequencer);
                ConsoleWrite(">>> Updated new Leader on view" + request.ViewId + " to leader " + request.ViewLeader, ConsoleColor.DarkGreen);
            }
            catch (Exception)
            {
                ConsoleWrite(">>> Failed to confirm leader on partition " + request.Pid, ConsoleColor.DarkRed);
            }
            return new Void { };
        }



        public override Task<Void> HeartBeat(Void request, ServerCallContext context)
        {
            return Task.FromResult(new Void { });
        }

        private void ConsoleWrite(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
