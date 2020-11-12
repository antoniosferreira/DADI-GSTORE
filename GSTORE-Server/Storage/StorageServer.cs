using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using GSTORE_Server.Communication;

namespace GSTORE_Server.Storage
{
    public class StorageServer
    {
        public string ServerID { get; }
        public int Delay { get; }

        private readonly ConcurrentDictionary<string, Partition> Partitions = new ConcurrentDictionary<string, Partition>();
        private readonly ConcurrentDictionary<string, Sequentiator> Sequentiators = new ConcurrentDictionary<string, Sequentiator>();
        
        private static readonly NodesCommunicator NodesCommunicator = new NodesCommunicator();

        private static readonly object WriteLock = new object();

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

            (string value,_) = Partitions[partitionID].GetValue(objectID);
            return (true, value);
        }

        // WRITE OPERATION TO CLIENT
        public (bool, string) Write(string partitionID, string objectID, string value)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            if (!Partitions.ContainsKey(partitionID))
                return (false, "-1");

            try
            {
                // STARTS WRITING ALGORITHM
                // LOCKS FURTHER WRITES
                lock (WriteLock)
                {
                    // GETS TID FROM SEQUENTIATOR
                    TIDRequest tidRequest = new TIDRequest
                    {
                        ObjectID = objectID,
                        PartitionID = partitionID
                    };
                    TIDReply tidReply = NodesCommunicator.GetServerClient(Partitions[partitionID].MasterServerID).GetTID(tidRequest);


                    // Sends Lock to Every Server
                    LockObjectRequest lockRequest = new LockObjectRequest
                    {
                        PartitionID = partitionID,
                        ObjectID = objectID
                    };
                    List<Task> requestTasks = new List<Task>();
                    foreach (string serverID in Partitions[partitionID].AssociatedServers)
                    {
                        void p()
                        {
                            try
                            {
                                Void reply = NodesCommunicator.GetServerClient(serverID).LockObject(lockRequest);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine(">>> SERVER {0} FAILED", serverID);
                                NodesCommunicator.DeactivateServer(serverID);
                                Partitions[partitionID].AssociatedServers.Remove(serverID);
                            }
                        }

                        Action action = p;
                        Task task = new Task(action);
                        requestTasks.Add(task);
                        task.Start();
                    }
                    Task.WaitAll(requestTasks.ToArray());


                    // Sends Write to Every Server
                    WriteObjectRequest writeRequest = new WriteObjectRequest
                    {
                        PartitionID = partitionID,
                        ObjectID = objectID,
                        Value = value,
                        TID = tidReply.TID
                    };

                    List<Task> writeTasks = new List<Task>();
                    foreach (string serverID in Partitions[partitionID].AssociatedServers)
                    {
                        void action() { 
                            try
                            {
                                Void reply = NodesCommunicator.GetServerClient(serverID).WriteObject(writeRequest);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine(">>> SERVER {0} FAILED", serverID);
                                NodesCommunicator.DeactivateServer(serverID);
                                Partitions[partitionID].AssociatedServers.Remove(serverID);
                            }
                        }

                        Task task = new Task(action);
                        writeTasks.Add(task);
                        task.Start();
                    }
                    Task.WaitAll(writeTasks.ToArray());
                }

                // Write CONFIRMED
                return (true, "-1");

            } catch (Exception e)
            {
                // Something happened with server communication
                Console.WriteLine(">>>Failed to write");
                Console.WriteLine(e.StackTrace);
            }
            
            
            return (false, "-1");
        }


        // Inites a new  partition
        public void NewPartition(string pid, List<string> servers)
        {
            CheckFreezeLock();

            Partition partition = new Partition(servers[0], servers);

            if (!Partitions.TryAdd(pid, partition))
                Partitions[pid] = partition;

            Sequentiators.AddOrUpdate(pid, new Sequentiator(), (k,v) => v = new Sequentiator());
        }



        // Locks some object in given partition, prior to writing to it
        public void LockObject(string partitionID, string objectID)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            Partitions[partitionID].LockValue(objectID);
        }


        // Writes some object, and unlocks it immediately
        public void WriteObject(string partitionID, string objectID, string value, int tid)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            if (tid < Sequentiators[partitionID].GetCurrentTID(objectID))
                return; 

            Partitions[partitionID].AddItem(objectID, value, tid);
            Sequentiators[partitionID].UpdateTID(objectID, tid);
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

            List<string> listing = new List<string>();

            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                string partitionListing = "=== Partition " + part.Key + " with master server " + part.Value.MasterServerID + " ===\n";
                foreach (KeyValuePair<string, Item> kvp in part.Value.Items)
                    partitionListing += "ObjectID = " + kvp.Key + " Value = " + kvp.Value.GetValue() + " TID:" + kvp.Value.GetTID() + "\n";
                listing.Add(partitionListing);
            }

            return listing;
        }


        public void PrintStatus()
        {
            SimulateCommunicationDelay();

            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                Console.WriteLine("=====Partition " + part.Key + " With Master Server " + part.Value.MasterServerID + " ======");
                foreach (KeyValuePair<string, Item> kvp in part.Value.Items)
                    Console.WriteLine("ObjectID = {0}, Value = {1}, TID = {2}", kvp.Key, kvp.Value.GetValue(), kvp.Value.GetTID());
            }
            foreach (string s in Sequentiators.Keys)
            {
                Console.WriteLine("/////////////  SEQUENTIATOR " + s + "///////////////");
                Console.WriteLine(Sequentiators[s].ListSequentiator());
            }
        }


        private void SimulateCommunicationDelay()
        {
            if (Delay > 0)
                Thread.Sleep(Delay);
        }


        public int GetNewTID(string pid, string oid)
        {
            if (Sequentiators.ContainsKey(pid))
                return Sequentiators[pid].GetTID(oid);

            return -1;
        }
    }
}
