using System;

using System.Threading;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections.Concurrent;

using GSTORE_Server.Communication;

namespace GSTORE_Server.Storage
{


    public class StorageServer
    {
        public string ServerID { get; }
        public int Delay { get; }
        private static readonly NodesCommunicator NodesCommunicator = new NodesCommunicator();


        private readonly ConcurrentDictionary<string, Partition> Partitions = new ConcurrentDictionary<string, Partition>();
        private readonly ConcurrentDictionary<string, object> PartitionLockers = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<string, bool> Elections = new ConcurrentDictionary<string, bool>();

        readonly object FreezeLock = new object();
        private bool Frozen = false;

        public StorageServer(string serverID, int delay) {
            ServerID = serverID;
            Delay = delay;


            // Thread for checking alive servers
            void p() {
                do
                {
                    Thread.Sleep(10000);
                    CheckLeaderHeartbeat();
                } while (true);
            };
            Action action = p;
            Task task = new Task(p);
            task.Start();
        }


        /* 
         * CLIENT EXPOSED OPERATIONS
         * Read, Write and List
        */


        // Returns immediate value if it is on storage
        public (bool, string) Read(string partitionID, string objectID)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            // This server doesn't replicate the partition
            if (!Partitions.ContainsKey(partitionID))
                return (false, "-1");

            return (true, Partitions[partitionID].GetValue(objectID));
        }

        // Contacts replica leader to start writing
        public bool Write(WriteData request)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            if (!Partitions.ContainsKey(request.Pid))
                return false;

            // Creates the Write Request
            WriteRequestData writeRequest = new WriteRequestData
            {
                Tid = -1, // to be assigned by leader partition
                Pid = request.Pid,
                Oid = request.Oid,
                Value = request.Value
            };


            // Forwards request to partition leader
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
                    // Leader is down
                    StartElections(request.Pid);
                }
            } while (reply == null || attempts < 20);

            return false;
        }

        // Lists every partition it replicates
        public List<string> ListServerPartitions()
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            List<string> listing = new List<string>();

            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                string partitionListing = "=== Partition " + part.Key + " with master server " + part.Value.CurrentView.ViewLeader + " ===\n";
                foreach (KeyValuePair<string, (int, string)> kvp in part.Value.Items)
                    partitionListing += "ObjectID = " + kvp.Key + " Value = " + kvp.Value.Item2 + "-TID-" + kvp.Value.Item1 + "\n";
                listing.Add(partitionListing);
            }

            return listing;
        }

        /* 
         * PM EXPOSED OPERATIONS
         * NewPartition, Freeze, Status
        */

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
                    Console.WriteLine("ObjectID = {0}, Value = {1}, TID = {2}", kvp.Key, kvp.Value.Item2, kvp.Value.Item1);

                Console.WriteLine("===== Sequencer ======");
                Console.WriteLine("Sequencer = {0}", part.Value.Sequencer);

                Console.WriteLine("===== View ======");
                Console.WriteLine("View = {0}", part.Value.CurrentView.ViewID);
                Console.WriteLine("Leader = {0}", part.Value.CurrentView.ViewLeader);
                foreach (string p in part.Value.CurrentView.ViewParticipants)
                    Console.Write("-{0}-", p);

            }
        }

        /* 
        * SYSTEM FUNCTIONALITY
        * LAUNCHWRITE, RETRIEVEWRITE
        */
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
                tid = partition.Sequencer += 1;

                request = new WriteRequestData
                {
                    Tid = tid,
                    Pid = write.Pid,
                    Oid = write.Oid,
                    Value = write.Value
                };

                // LOCAL WRITE
                Partitions[write.Pid].AddItem(new WriteData(request));
            }
            

            List<Task> requestTasks = new List<Task>();

            List<string> failedServers = new List<string>();
            
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

            return true;
        }

        public WriteData RetrieveWrite(string pid, int tid, int viewId)
        {
            if (Partitions[pid].CurrentView.ViewID == viewId)
                return Partitions[pid].CurrentView.Buffer[tid];
            if (Partitions[pid].PreviousView.ViewID == viewId)
                return Partitions[pid].PreviousView.Buffer[tid];

            // Not expected to happen
            ConsoleWrite("StorageServer: Failure on Retrievewrite, functionality compromised", ConsoleColor.DarkRed);
            return null;
        }



        /* 
         * VIEW COMMUNICATION
         * STARTNEWVIEW, PROCESSVIEWCHANGE, PROCESSVIEWDELIVERY
         */

        private void StartNewView(Partition partition, List<string> failedServers)
        {

            lock (PartitionLockers[partition.PartitionID])
            {
                // Updates LOCALLY
                partition.PreviousView = partition.CurrentView;
                partition.CurrentView = new View(partition.CurrentView, failedServers);
                Elections[partition.PartitionID] = false;

                // Updates REMOTELY
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
                                Pid = partition.PartitionID,
                                ViewSequencer = partition.Sequencer
                            };

                            changeRequest.ViewParticipants.Add(partition.CurrentView.ViewParticipants);
                            Void reply = NodesCommunicator.GetServerClient(server).ViewChange(changeRequest);
                        }
                        catch (Exception)
                        {
                            ConsoleWrite("StorageServer: Server " + server + " failed on receiving view change", ConsoleColor.DarkRed);
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


        public void ProcessViewDelivery(int viewID, string viewLeader, WriteData write)
        {
            lock (PartitionLockers[write.Pid])
            {
                if (viewID == Partitions[write.Pid].CurrentView.ViewID && viewLeader.Equals(Partitions[write.Pid].CurrentView.ViewLeader))
                    Partitions[write.Pid].AddItem(write);
            }

        }


        public void ProcessViewChange(string pid, int viewId, string viewLeader, List<String> viewParticipants, int viewSequencer)
        {
            lock (PartitionLockers[pid])
            {
                // Starts by retrieving old writes from last view
                // Every server has every message on a view before moving on 
                if (Partitions[pid].Sequencer < viewSequencer)
                {
                    int lastRequest = Partitions[pid].Sequencer;
                    do
                    {
                        lastRequest += 1;

                        // I'm updated now
                        if (lastRequest == viewSequencer)
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
                                Partitions[pid].Sequencer += 1;
                                success = true;

                                break;
                            }
                            catch (Exception)
                            {
                                // Some other server is failing...
                                // Leader it will take care of it
                            }
                        }

                        // Failed to retrieve some write
                        // Functionality compromised
                        if (!success) Environment.Exit(0);
                    } while (lastRequest < viewSequencer);
                }


                // Updates View
                Partitions[pid].PreviousView = Partitions[pid].CurrentView;
                Partitions[pid].CurrentView = new View(viewId, viewLeader, viewParticipants);
                Elections[pid] = false;
            }

        }


        /* 
        * FAULT TOLERANCE
        * ELECTIONS, HEARTBEATS
        */
        public void ProcessElection(string pid, string sid)
        {
            // Im going to be the leader
            if (ServerID.Equals(sid))
            {
                // Updates new view locally
                List<string> participants = Partitions[pid].CurrentView.ViewParticipants;
                Partitions[pid].PreviousView = Partitions[pid].CurrentView;
                Partitions[pid].CurrentView = new View(Partitions[pid].PreviousView.ViewID + 1, ServerID, participants);

                // Updates new view remotely
                ViewChangeRequest confirmation = new ViewChangeRequest
                {
                    Pid = pid,
                    ViewId = Partitions[pid].CurrentView.ViewID,
                    ViewLeader = sid,
                    ViewSequencer = Partitions[pid].Sequencer
                };
                confirmation.ViewParticipants.Add(participants);

                foreach (string participant in Partitions[pid].CurrentView.ViewParticipants)
                {
                    try
                    {
                        NodesCommunicator.GetServerClient(participant).ConfirmLeader(confirmation);
                    }
                    catch (Exception)
                    {
                        // do what? lol
                    }
                }

                return;
            }


            // PARTICIPATING ON ELECTIONS
            LeaderElectionRequest request;
            if (String.Compare(ServerID, sid) < 0)
                request = new LeaderElectionRequest { Pid = pid, Sid = ServerID };
            else
                request = new LeaderElectionRequest { Pid = pid, Sid = sid };

            int attempts = 0;
            do
            {
                int pos = (Partitions[pid].CurrentView.ViewParticipants.IndexOf(ServerID) + 1 + attempts) % Partitions[pid].CurrentView.ViewParticipants.Count;
                attempts += 1;

                try
                {
                    NodesCommunicator.GetServerClient(Partitions[pid].CurrentView.ViewParticipants[pos]).ElectLeader(request);
                    break;
                }
                catch (Exception)
                {
                    // another participant is down, just keep going
                }
            } while (attempts < Partitions[pid].CurrentView.ViewParticipants.Count);

        }

        public void StartElections(string pid)
        {
            ConsoleWrite("StorageServer: Starting elections on partition " + pid, ConsoleColor.DarkMagenta);
            Elections[pid] = true;
            int electionAttempts = 0;

            List<string> viewServers = Partitions[pid].CurrentView.ViewParticipants;
            viewServers.Remove(Partitions[pid].CurrentView.ViewLeader);
            do
            {
                int pos = (viewServers.IndexOf(ServerID) + 1 + electionAttempts) % viewServers.Count;
                electionAttempts += 1;

                try
                {
                    string sid = viewServers[pos];

                    LeaderElectionRequest electionRequest = new LeaderElectionRequest { Pid = pid, Sid = sid };
                    NodesCommunicator.GetServerClient(sid).ElectLeader(electionRequest);
                    break;
                }
                catch (Exception)
                {
                    // another participant is down, just keep going
                }
            } while (electionAttempts < viewServers.Count);
        }

        public void CheckLeaderHeartbeat()
        {
            ConsoleWrite("Heartbeating leaders", ConsoleColor.DarkCyan);
            foreach (Partition partition in Partitions.Values)
            {
                if (!Elections[partition.PartitionID])
                {
                    string leader = partition.CurrentView.ViewLeader;
                    try
                    {
                        NodesCommunicator.GetServerClient(partition.CurrentView.ViewLeader).HeartBeat(new Void { });
                        ConsoleWrite("Leader " + leader + " on partition " + partition.PartitionID + " still alive", ConsoleColor.DarkCyan);
                    }
                    catch (Exception)
                    {
                        ConsoleWrite("Leader " + leader + " on partition " + partition.PartitionID + " is dead", ConsoleColor.DarkRed);

                        // Leader is down, start elections
                        Elections[partition.PartitionID] = true;
                        int electionAttempts = 0;

                        List<string> viewServers = Partitions[partition.PartitionID].CurrentView.ViewParticipants;
                        viewServers.Remove(Partitions[partition.PartitionID].CurrentView.ViewLeader);
                        do
                        {
                            int pos = (viewServers.IndexOf(ServerID) + 1 + electionAttempts) % viewServers.Count;
                            electionAttempts += 1;

                            try
                            {
                                string sid = viewServers[pos];

                                LeaderElectionRequest electionRequest = new LeaderElectionRequest { Pid = partition.PartitionID, Sid = sid };
                                NodesCommunicator.GetServerClient(sid).ElectLeader(electionRequest);
                                break;
                            }
                            catch (Exception)
                            {
                                // another participant is down, just keep going
                            }
                        } while (electionAttempts < viewServers.Count);
                    }
                }
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
