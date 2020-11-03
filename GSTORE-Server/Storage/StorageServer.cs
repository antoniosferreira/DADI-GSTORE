using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace GSTORE_Server.Storage
{
    public class StorageServer
    {
        public string ServerID { get; }
        public int Delay { get; }


        // Partitions stored 
        private ConcurrentDictionary<string, Partition> Partitions = new ConcurrentDictionary<string, Partition>();
        static private NodesCommunicator NodesCommunicator = new NodesCommunicator();

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

            // Partition don't store the object
            if (!Partitions[partitionID].Storage.ContainsKey(objectID))
                return (false, "N/A");

            return Partitions[partitionID].GetValue(objectID);
        }

        // WRITE OPERATION TO CLIENT
        public (bool, string) Write(string partitionID, string objectID, string value)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            if (!Partitions.ContainsKey(partitionID))
                return (false, "-1");


            if (!Partitions[partitionID].MasterServerID.Equals(ServerID))
            {
                // This server is not the master, but we can
                // redirect the client to the right server
                return (false, Partitions[partitionID].MasterServerID);
            }
            else
            {
                try
                {
                    // STARTS WRITING ALGORITHM
                    // LOCKS FURTHER WRITES
                    lock (WriteLock)
                    {
                        // Sends Lock to Every Server
                        int writeID = (new Random()).Next(0, 1000);
                        LockObjectRequest lockRequest = new LockObjectRequest
                        {
                            PartitionID = partitionID,
                            ObjectID = objectID,
                            WriteID = writeID
                        };
                        List<Task> requestTasks = new List<Task>();
                        foreach (string serverID in Partitions[partitionID].AssociatedServers)
                        {
                            Action action = () =>
                                {
                                    LockObjectReply reply = NodesCommunicator.GetServerClient(serverID).LockObject(lockRequest);
                                };
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
                            WriteID = writeID
                        };
                        List<Task> writeTasks = new List<Task>();
                        foreach (string serverID in Partitions[partitionID].AssociatedServers)
                        {
                            Action action = () => { WriteObjectReply reply = NodesCommunicator.GetServerClient(serverID).WriteObject(writeRequest); };
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
                    Console.WriteLine("Failed to write");
                    Console.WriteLine(e.StackTrace);
                }
            } 
            
            return (false, "-1");
        }


        // Creates a new local partition
        public void NewPartition(string pid, List<string> servers)
        {
            CheckFreezeLock();

            Partition partition = new Partition(ServerID, servers);

            if (!Partitions.TryAdd(pid, partition))
                Partitions[pid] = partition;
        }


        // Creates a local replicate of a remote partition
        public void InitPartition(string pid, string sid, List<string> servers)
        {
            Partition partition = new Partition(sid, servers);
            Partitions.TryAdd(pid, partition);
        }


        // Starts replication of local partitions 
        public void Replication(int rfactor)
        {

            foreach (KeyValuePair<string, Partition> kvp in Partitions)
                // For every Master Local Partition
                if (kvp.Value.MasterServerID.Equals(ServerID))
                {
                    // Send requests to associated servers
                    foreach (string serverID in kvp.Value.AssociatedServers)
                    {
                        SetPartitionRequest request = new SetPartitionRequest
                        {
                            PartitionID = kvp.Key,
                            MainServerID = kvp.Value.MasterServerID,
                        };
                        request.AssociatedServers.Add(kvp.Value.AssociatedServers);

                        NodesCommunicator.GetServerClient(serverID).SetPartitionAsync(request);
                    }
                }

        }


        // Locks some object in given partition, prior to writing to it
        public void LockObject(string partitionID, string objectID, int writeID)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            Partitions[partitionID].LockValue(objectID, writeID);
        }


        // Writes some object, and unlocks it immediately
        public void WriteObject(string partitionID, string objectID, string value, int writeID)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            Partitions[partitionID].AddKeyPair(objectID, value, writeID);
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
                foreach (KeyValuePair<string, string> kvp in part.Value.Storage)
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
                Console.WriteLine("=====Partition " + part.Key + " With Master Server " + part.Value.MasterServerID + " ======");
                foreach (KeyValuePair<string, string> kvp in part.Value.Storage)
                    Console.WriteLine("ObjectID = {0}, Value = {1}", kvp.Key, kvp.Value);
            }
        }


        private void SimulateCommunicationDelay()
        {
            if (Delay > 0)
                Thread.Sleep(Delay);
        }

    }
}
