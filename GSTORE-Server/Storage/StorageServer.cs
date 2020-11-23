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

        private readonly ConcurrentDictionary<string, Partition> Partitions = new ConcurrentDictionary<string, Partition>();
        private readonly ConcurrentDictionary<string, int> Sequencers = new ConcurrentDictionary<string, int>();
        
        private static readonly NodesCommunicator NodesCommunicator = new NodesCommunicator();

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
        public (bool, string) Write(WriteData request)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            if (!Partitions.ContainsKey(request.Pid))
                return (false, "-1");

            // If replicates the partition, starts the process by forwarding the request 
            // to partition's leader
            WriteRequestData writeRequest = new WriteRequestData
            {
                Tid = -1, // to be assigned by leader partition
                Oid = request.Oid,
                Pid = request.Pid,
                Value = request.Value
            };

            WriteResult reply = null;
            do
            {
                try
                {
                    Console.WriteLine("HERE1");

                    if (Partitions[request.Pid].PartitionLeader != null)
                        return ((NodesCommunicator.GetServerClient(Partitions[request.Pid].PartitionLeader).StartWrite(writeRequest)).Success, "-1");
                    else 
                        Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    // Leader is down, start elections
                    Partitions[request.Pid].PartitionLeader = null;
                    int attempts = 0;

                    do
                    {
                        int pos = (Partitions[request.Pid].ReplicatedServers.IndexOf(ServerID) + 1 + attempts) % Partitions[request.Pid].ReplicatedServers.Count;
                        attempts += 1;

                        try
                        {
                            string sid = Partitions[request.Pid].ReplicatedServers[pos];

                            LeaderElectionRequest electionRequest = new LeaderElectionRequest { Pid = request.Pid, Sid = sid };
                            NodesCommunicator.GetServerClient(sid).ElectLeader(electionRequest);
                            break;
                        }
                        catch (Exception)
                        {
                            // another participant is down, just keep going
                        }
                    } while (attempts < Partitions[request.Pid].ReplicatedServers.Count);
                }


            } while (reply == null);

            return (false, "-1");
        }


        // Inites a new  partition
        public void NewPartition(string pid, List<string> servers)
        {
            CheckFreezeLock();

            Partition partition = new Partition(servers[0], servers);

            if (!Partitions.TryAdd(pid, partition))
                Partitions[pid] = partition;

            Sequencers.AddOrUpdate(pid, 0, (k,v) => v = 0);
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

        public List<string> ListServerPartitions()
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            List<string> listing = new List<string>();

            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                string partitionListing = "=== Partition " + part.Key + " with master server " + part.Value.PartitionLeader + " ===\n";
                foreach (KeyValuePair<string, string> kvp in part.Value.Items)
                    partitionListing += "ObjectID = " + kvp.Key + " Value = " + kvp.Value + "\n";
                listing.Add(partitionListing);
            }

            return listing;
        }


        public void PrintStatus()
        {
            SimulateCommunicationDelay();

            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                Console.WriteLine("=====Partition " + part.Key + " With Master Server " + part.Value.PartitionLeader + " ======");
                foreach (KeyValuePair<string, string> kvp in part.Value.Items)
                    Console.WriteLine("ObjectID = {0}, Value = {1}", kvp.Key, kvp.Value);
            }
            foreach (string s in Sequencers.Keys)
            {
                Console.WriteLine("/////////////  SEQUENCER " + s + "///////////////");
                Console.WriteLine("Sequencer " + s + " Value " + Sequencers[s]);
            }
        }


        private void SimulateCommunicationDelay()
        {
            if (Delay > 0)
                Thread.Sleep(Delay);
        }


        public int GetNewTID(string pid)
        {
            return Sequencers[pid] += 1;
        }


        public void ProcessElection(string pid, string sid)
        {
            // Im going to be the leader
            Console.WriteLine("Comparation" + ServerID.Equals(sid));
            if (ServerID.Equals(sid))
            {
                Console.WriteLine("T1");
                LeaderConfirmationRequest confirmation = new LeaderConfirmationRequest { Sid = sid, Pid = pid};
                foreach (string participant in Partitions[pid].ReplicatedServers)
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
            Partitions[pid].PartitionLeader = null;

            LeaderElectionRequest request;
            if (String.Compare(ServerID, sid) < 0)
            {
                request = new LeaderElectionRequest { Pid = pid, Sid = ServerID };
            } else
            {
                request = new LeaderElectionRequest { Pid = pid, Sid = sid };
            }
            Console.WriteLine("T3");

            int attempts = 0;
            do
            {
                int pos = (Partitions[pid].ReplicatedServers.IndexOf(ServerID) + 1 + attempts) % Partitions[pid].ReplicatedServers.Count;
                attempts += 1;

                try
                {
                    Console.WriteLine("T4");
                    NodesCommunicator.GetServerClient(Partitions[pid].ReplicatedServers[pos]).ElectLeader(request);
                    break;
                }
                catch (Exception)
                {
                    // another participant is down, just keep going
                }
            } while (attempts < Partitions[pid].ReplicatedServers.Count);
            Console.WriteLine("T5");

        }


        public void ProcessLeaderConfirmation(string pid, string sid)
        {
            Partitions[pid].PartitionLeader = sid;
        }


        public void CheckAliveServers() {

            // Contacts Every Server on Nodes
            // Sends heartbeat
            // checks every partition for that server, removes it
            foreach (Tuple<String,ServerCommunicationServices.ServerCommunicationServicesClient> tuple in NodesCommunicator.GetServers())
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
                            if (p.ReplicatedServers.Contains(tuple.Item1))
                            {
                                p.ReplicatedServers.Remove(tuple.Item1);
                            }
                        }
                    }
                }

                Task task = new Task(action);
                task.Start();
            }
        }


        public bool StartWrite(WriteData write)
        {
            if (!Partitions.ContainsKey(write.Pid))
                return false;

            int tid = GetNewTID(write.Pid);

            WriteRequestData request = new WriteRequestData
            {
                Tid = tid,
                Pid = write.Pid,
                Oid = write.Oid,
                Value = write.Value
            };

            // Sends prepare request to all replicas
            List<Task> requestTasks = new List<Task>();
            foreach (string serverID in Partitions[write.Pid].ReplicatedServers)
            {

                void p()
                {
                    try
                    {
                        Void reply = NodesCommunicator.GetServerClient(serverID).DeliverPrepareRequest(request);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(">>> SERVER {0} FAILED TO PREPARE WRITE", serverID);
                        //NodesCommunicator.DeactivateServer(serverID);
                        //Partitions[write.Pid].AssociatedServers.Remove(serverID);
                        //Console.WriteLine(e.StackTrace);
                    }
                }

                Console.WriteLine(serverID);
                Action action = p;
                Task task = new Task(action);
                requestTasks.Add(task);
                task.Start();
            }
            Task.WaitAll(requestTasks.ToArray());
            

            return true;
        }


        public void DeliverPrepareRequest(WriteData write)
        {
            if (!Partitions.ContainsKey(write.Pid))
                return;



            // CONFIRMS IT IS UPDATED
            if (!Partitions[write.Pid].Buffer.ContainsKey(write.Tid - 1))
            {
                int lastRequest = 0;
                if (Partitions[write.Pid].Buffer.Keys.Count > 0)
                    lastRequest = Partitions[write.Pid].Buffer.Keys.Max();
                do
                {
                    lastRequest += 1;
                    Console.WriteLine("NEED TO RETRIEVE" + lastRequest);
                    if (lastRequest == write.Tid)
                        break;

                    foreach (string serverID in Partitions[write.Pid].ReplicatedServers)
                    {
                        try
                        {
                            Console.WriteLine("Retrieving write" + write.Tid);
                            WriteRequestData reply = NodesCommunicator.GetServerClient(serverID).RetrieveWrite(new WriteRetrievalRequest { Tid = lastRequest, Pid = write.Pid }); ;
                            WriteData previousWrite = new WriteData(reply);
                            Partitions[write.Pid].AddItem(previousWrite);
                            Sequencers[write.Pid] = previousWrite.Tid;

                            break;
                        }
                        catch (Exception)
                        {
                            //
                        }
                    }
                } while (lastRequest < write.Tid);
            }

            if (ServerID.Equals("s3"))
                return;
            Partitions[write.Pid].AddItem(write);
            Sequencers[write.Pid] = write.Tid;
        }


        public WriteData RetrieveWrite(int tid, string pid)
        {
            Console.WriteLine("Sending write" + tid);
            if (Partitions[pid].Buffer.ContainsKey(tid))
                return Partitions[pid].Buffer[tid];

            return null;
        }
    }
}
