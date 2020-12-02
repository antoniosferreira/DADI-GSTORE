using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GSTORE_Server.Communication;
using System.Linq;

namespace GSTORE_Server.Storage
{
    public class StorageServer
    {
        public string ServerID { get; }
        public int Delay { get; }
        private static readonly NodesCommunicator NodesCommunicator = new NodesCommunicator();


        private readonly ConcurrentDictionary<string, Partition> Partitions = new ConcurrentDictionary<string, Partition>();
        private readonly ConcurrentDictionary<string, Object> PartitionLockers = new ConcurrentDictionary<string, Object>();
        private readonly ConcurrentDictionary<string, bool> Elections = new ConcurrentDictionary<string, bool>();

        readonly object FreezeLock = new object();
        private bool Frozen = false;


        public StorageServer(string serverID, int delay) {
            ServerID = serverID;
            Delay = delay;
        }


        // READ OPERATION TO CLIENT
        public (bool, string) Read(string partitionID, string objectID)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            // This server doesn't replicate the partition
            if (!Partitions.ContainsKey(partitionID))
                return (false, "-1");

            return (true, Partitions[partitionID].GetValue(objectID));
        }

        // WRITE OPERATION TO CLIENT
        public bool Write(WriteData request)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            if (!Partitions.ContainsKey(request.Pid))
                return false;

            // If replicates the partition, starts the write by forwarding the request 
            // to partition's leader
            WriteRequestData writeRequest = new WriteRequestData
            {
                Tid = -1, // to be assigned by leader partition
                Pid = request.Pid,
                Oid = request.Oid,
                Value = request.Value
            };

            int attempts = 0;
            WriteResult reply = null;
            do
            {
                try
                {
                    string partitionLeader = Partitions[request.Pid].CurrentView.ViewLeader;
                    if (partitionLeader != null)
                    {
                        attempts += 1;
                        return NodesCommunicator.GetServerClient(partitionLeader).LaunchWrite(writeRequest).Success;
                    }
                    else
                        Thread.Sleep(500);
                }
                catch (Exception)
                {
                    // Leader is down, start elections
                    Elections[request.Pid] = true;
                    int electionAttempts = 0;

                    List<string> viewServers = Partitions[request.Pid].CurrentView.ViewParticipants;
                    viewServers.Remove(Partitions[request.Pid].CurrentView.ViewLeader);
                    do
                    {
                        int pos = (viewServers.IndexOf(ServerID) + 1 + electionAttempts) % viewServers.Count;
                        electionAttempts += 1;

                        try
                        {
                            string sid = viewServers[pos];

                            LeaderElectionRequest electionRequest = new LeaderElectionRequest { Pid = request.Pid, Sid = sid };
                            NodesCommunicator.GetServerClient(sid).ElectLeader(electionRequest);
                            break;
                        }
                        catch (Exception)
                        {
                            // another participant is down, just keep going
                        }
                    } while (attempts < viewServers.Count);
                }
            } while (reply == null || attempts < 20);


            return false;
        }

        // LIST OPERATION TO CLIENT
        public List<string> ListServerPartitions()
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            List<string> listing = new List<string>();

            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                string partitionListing = "=== Partition " + part.Key + " with master server " + part.Value.CurrentView.ViewLeader + " ===\n";
                foreach (KeyValuePair<string, (int, string)> kvp in part.Value.Items)
                    partitionListing += "ObjectID = " + kvp.Key + " Value = " + kvp.Value.Item2 + "-" + kvp.Value.Item1 + "\n";
                listing.Add(partitionListing);
            }

            return listing;
        }

        //
        // System Configuration
        //
        //  NewPartition, Freezes, Status
        //

        public void NewPartition(string pid, List<string> servers)
        {
            CheckFreezeLock();

            Partition partition = new Partition(pid, servers);
            Partitions.AddOrUpdate(pid, partition, (k, v) => v = partition);
            PartitionLockers.AddOrUpdate(pid, new Object(), (k, v) => new object());
            Elections.AddOrUpdate(pid, false, (k, v) => false);
        }

        private void SimulateCommunicationDelay()
        {
            if (Delay > 0)
                Thread.Sleep(Delay);
        }

        private void CheckFreezeLock()
        {
            lock (FreezeLock)
                while (Frozen)
                    Monitor.Wait(FreezeLock);
        }
        public void Freeze()
        {
            lock (FreezeLock)
                Frozen = true;
        }
        public void Unfreeze()
        {
            lock (FreezeLock)
            {
                Frozen = false;
                Monitor.PulseAll(FreezeLock);
            }
        }

        public void PrintStatus()
        {
            SimulateCommunicationDelay();

            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                Console.WriteLine("===== Partition " + part.Key + " With Master Server " + part.Value.CurrentView.ViewLeader + " ======");
                foreach (KeyValuePair<string, (int,string)> kvp in part.Value.Items)
                    Console.WriteLine("ObjectID = {0}, Value = {1}", kvp.Key, kvp.Value.Item2);

                Console.WriteLine("===== Sequencers ======");
                foreach (KeyValuePair<string, int> seq in part.Value.ReplicasSequencers)
                {
                    Console.WriteLine("Server = {0}  |  Tid = {1}", seq.Key, seq.Value);
                }
            }
        }


        //
        // Communication Configuration
        //
        //  Elections, heartbeats
        //
        public void ProcessElection(string pid, string sid)
        {
            // Im going to be the leader
            if (ServerID.Equals(sid))
            {

                // update new view locally
                List<string> participants = Partitions[pid].CurrentView.ViewParticipants;
                Partitions[pid].PreviousView = Partitions[pid].CurrentView;
                Partitions[pid].CurrentView = new View(Partitions[pid].PreviousView.ViewID+1, ServerID, participants);
                Partitions[pid].ReplicasSequencers[ServerID] = Partitions[pid].ReplicasSequencers[Partitions[pid].PreviousView.ViewLeader];
                
                // updates new view remotely
                ViewChangeRequest confirmation = new ViewChangeRequest { 
                    Pid = pid,
                    ViewId = Partitions[pid].CurrentView.ViewID,
                    ViewLeader = sid
                };

                confirmation.ViewParticipants.Add(participants);
                List<ViewSequencers> sequencers = new List<ViewSequencers>();
                foreach (KeyValuePair<string, int> kvp in Partitions[pid].ReplicasSequencers)
                {
                    ViewSequencers seq = new ViewSequencers
                    {
                        Sid = kvp.Key,
                        Sequencer = kvp.Value
                    };
                    sequencers.Add(seq);
                }
                confirmation.ViewSequencers.Add(sequencers);

                foreach (string participant in Partitions[pid].CurrentView.ViewParticipants)
                {
                    try
                    {
                        NodesCommunicator.GetServerClient(participant).ConfirmLeader(confirmation);
                    } catch (Exception)
                    {
                        // do what? lol
                    }
                }

                return;
            }


            // PARTICIPATING ON ELECTIONS

            LeaderElectionRequest request;
            if (String.Compare(ServerID, sid) < 0)
            {
                request = new LeaderElectionRequest { Pid = pid, Sid = ServerID };
            } else
            {
                request = new LeaderElectionRequest { Pid = pid, Sid = sid };
            }

            int attempts = 0;
            do
            {
                int pos = (Partitions[pid].CurrentView.ViewParticipants.IndexOf(ServerID) + 1 + attempts) % Partitions[pid].CurrentView.ViewParticipants.Count;
                attempts += 1;

                try
                {
                    NodesCommunicator.GetServerClient(Partitions[pid].CurrentView
                        .ViewParticipants[pos]).ElectLeader(request);
                    break;
                }
                catch (Exception)
                {
                    // another participant is down, just keep going
                }
            } while (attempts < Partitions[pid].CurrentView.ViewParticipants.Count);

        }

        public void CheckAliveServers() {

            // Contacts Every Server on Nodes
            // Sends heartbeat
            // checks every partition for that server, removes it
            foreach (Tuple<String, ServerCommunicationServices.ServerCommunicationServicesClient> tuple in NodesCommunicator.GetServers())
            {
                void action()
                {
                    try
                    {
                        tuple.Item2.HeartBeat(new Void { });
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(">>> Server " + tuple.Item1 + " failed to respond to heartbeat");

                        // Removes server from each partition
                        foreach (Partition p in Partitions.Values)
                        {
                            if (p.CurrentView.ViewParticipants.Contains(tuple.Item1))
                            {
                                p.CurrentView.ViewParticipants.Remove(tuple.Item1);
                            }
                        }
                    }
                }

                Task task = new Task(action);
                task.Start();
            }
        }




        //
        // FUNCTIONALITY
        //  
        //  launchwrite, retrieves and deliveries
        //
        public bool LaunchWrite(WriteData write)
        {

            if (!Partitions.ContainsKey(write.Pid))
                return false;

            Partition partition = Partitions[write.Pid];

            // Creates Write Request To Other Replicas
            int tid = -1;

            WriteRequestData request;

            lock (PartitionLockers[write.Pid])
            {
                tid = partition.ReplicasSequencers[ServerID] += 1;

                request = new WriteRequestData
                {
                    Tid = tid,
                    Pid = write.Pid,
                    Oid = write.Oid,
                    Value = write.Value
                };

                // write locally 
                Partitions[write.Pid].AddItem(new WriteData(request));
            }


            // Sends write to all replicas,
            // using a view communication call
            bool success = false;
            List<string> failedServers = new List<string>();

            List<Task> requestTasks = new List<Task>();
            List<string> participants = new List<string>(partition.CurrentView.ViewParticipants);
            participants.Remove(ServerID);
            foreach (string server in participants)
            {
                void p()
                {
                    try
                    {
                        ViewDeliverRequest deliverRequest = new ViewDeliverRequest
                        {
                            ViewId = partition.CurrentView.ViewID,
                            ViewLeader = ServerID,
                            Message = request
                        };

                        Void reply = NodesCommunicator.GetServerClient(server).ViewDeliver(deliverRequest);
                        success = true;
                    }
                    catch (Exception)
                    {
                        // Leader Failed To Deliver Write on Some Server
                        // Server will be excluded from next view
                        NodesCommunicator.DeactivateServer(server);
                        failedServers.Add(server);
                    }
                }

                Action action = p;
                Task task = new Task(action);
                requestTasks.Add(task);
                task.Start();
            }


            Task.WaitAll(requestTasks.ToArray());


            // Updates View if some server failed
            if (failedServers.Count > 0)
                StartNewView(partition, failedServers);

            return success;
        }


        public void ProcessViewDelivery(int viewID, string viewLeader, WriteData write)
        {
            lock (PartitionLockers[write.Pid])
            {
                if (viewID == Partitions[write.Pid].CurrentView.ViewID && viewLeader.Equals(Partitions[write.Pid].CurrentView.ViewLeader))
                    Partitions[write.Pid].AddItem(write);
                else
                {
                    Console.WriteLine("View delivery rejected");
                }
            }

        }


        public void ProcessViewChange(string pid, int viewId, string viewLeader, List<String> viewParticipants, List<(string,int)> viewSequencers)
        {
            lock (PartitionLockers[pid])
            {
                // Retrieves old writes
                foreach ((string,int) remoteSequencer in viewSequencers) { 
                    if (Partitions[pid].ReplicasSequencers[remoteSequencer.Item1]<remoteSequencer.Item2)
                    {
                        int lastRequest = Partitions[pid].ReplicasSequencers[remoteSequencer.Item1];
                        do
                        {
                            lastRequest += 1;
                            if (lastRequest == remoteSequencer.Item2)
                                break;

                            bool success = false;
                            foreach (string serverID in Partitions[pid].CurrentView.ViewParticipants)
                            {
                                try
                                {
                                    WriteRetrievalRequest retrieveRequest = new WriteRetrievalRequest
                                    {
                                        Tid = lastRequest,
                                        Pid = pid,
                                        ViewId = Partitions[pid].CurrentView.ViewID
                                    };
                                    WriteRequestData r = NodesCommunicator.GetServerClient(serverID).RetrieveWrite(retrieveRequest);

                                    // Process write
                                    Partitions[pid].AddItem(new WriteData(r));
                                    Partitions[pid].ReplicasSequencers[Partitions[pid].CurrentView.ViewLeader] += 1;
                                    success = true;

                                    break;
                                }
                                catch (Exception)
                                {
                                    // Some other server is failing...
                                    // Leader it will take care of it
                                }
                            }
                            // Failed to retrieve write
                            // Exits immed
                            if (!success) Environment.Exit(0);
                        } while (lastRequest < remoteSequencer.Item2);
                    }
                }

                // Updates View
                Partitions[pid].PreviousView = Partitions[pid].CurrentView;
                Partitions[pid].CurrentView = new View(viewId, viewLeader, viewParticipants);
            }

        }


        public WriteData RetrieveWrite(string pid, int tid, int viewId)
        {
            if (Partitions[pid].CurrentView.ViewID == viewId)
                return Partitions[pid].CurrentView.Buffer[tid];
            if (Partitions[pid].PreviousView.ViewID == viewId)
                return Partitions[pid].PreviousView.Buffer[tid];

            return null;
        }



        private void StartNewView(Partition partition, List<string> failedServers)
        {

            lock (PartitionLockers[partition.PartitionID])
            {
                // Updates LOCALLY
                partition.PreviousView = partition.CurrentView;
                partition.CurrentView = new View(partition.CurrentView, failedServers);

                // Updates REMOTELY
                List<ViewSequencers> sequencers = new List<ViewSequencers>();
                foreach (KeyValuePair<string, int> kvp in partition.ReplicasSequencers)
                {
                    ViewSequencers seq = new ViewSequencers
                    {
                        Sid = kvp.Key,
                        Sequencer = kvp.Value
                    };
                    sequencers.Add(seq);
                }
                List<string> servers = partition.CurrentView.ViewParticipants;
                servers.Remove(ServerID);
                List<Task> requestTasks = new List<Task>();
                foreach (string server in servers)
                {
                    void p()
                    {
                        try
                        {
                            ViewChangeRequest changeRequest = new ViewChangeRequest
                            {
                                ViewId = partition.CurrentView.ViewID,
                                ViewLeader = ServerID,
                                Pid = partition.PartitionID
                            };

                            changeRequest.ViewParticipants.Add(partition.CurrentView.ViewParticipants);
                            changeRequest.ViewSequencers.Add(sequencers);
                            Void reply = NodesCommunicator.GetServerClient(server).ViewChange(changeRequest);
                        }
                        catch (Exception)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine(">>> SERVER {0} FAILED TO DELIVER VIEW CHANGE", server);
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                    }

                    Action action = p;
                    Task task = new Task(action);
                    requestTasks.Add(task);
                    task.Start();
                }

                Task.WaitAll(requestTasks.ToArray());

            }
        }

        private void ConsoleWrite(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
