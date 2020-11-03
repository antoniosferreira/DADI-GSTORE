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


        // Partitions that this server stores
        private ConcurrentDictionary<string, Partition> Partitions = new ConcurrentDictionary<string, Partition>();
        static private NodesCommunicator NodesCommunicator = new NodesCommunicator();


        private static readonly object WriteLock = new object();

        readonly object FreezeLock = new object();
        private bool Frozen = false;


        public StorageServer(string serverID, int delay) {
            ServerID = serverID;
            Delay = delay;
        }

        // GSTORE-CLIENT OPERATIONS
        public (bool, string) Read(string partitionID, string objectID)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            // This server doesn't replicate the partition
            if (!Partitions.ContainsKey(partitionID))
                return (false, "-1");

            // The partitions doesnt store the object
            if (!Partitions[partitionID].Storage.ContainsKey(objectID))
                return (false, "-1");

            return Partitions[partitionID].GetValue(objectID);
        }


        public (bool, string) Write(string partitionID, string objectID, string value)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            try
            {
                if (!Partitions.ContainsKey(partitionID))
                    return (false, "-1");

                if (Partitions[partitionID].MasterServerID.Equals(ServerID))
                {

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


                        try
                        {
                            List<Task> requestTasks = new List<Task>();
                            foreach (string serverID in Partitions[partitionID].AssociatedServers)
                            {
                                Action action = () => {
                                    try
                                    {
                                        LockObjectReply reply = NodesCommunicator.GetServerClient(serverID).LockObject(lockRequest);
                                    } catch (Exception e)
                                    {
                                        Console.WriteLine(e.StackTrace);
                                    }
                                };
                                Task task = new Task(action);
                                requestTasks.Add(task);
                                task.Start();
                            }
                            Task.WaitAll(requestTasks.ToArray());
                        } catch (Exception e) {
                            Console.WriteLine(e.StackTrace);
                        }



                        // Sends Write to Every Server
                        WriteObjectRequest writeRequest = new WriteObjectRequest
                        {
                            PartitionID = partitionID,
                            ObjectID = objectID,
                            Value = value,
                            WriteID = writeID
                        };

                        Console.WriteLine("LOCKING WRITES-4");

                        List<Task> writeTasks = new List<Task>();
                        foreach (string serverID in Partitions[partitionID].AssociatedServers)
                        {
                            Action action = () => { WriteObjectReply reply = NodesCommunicator.GetServerClient(serverID).WriteObject(writeRequest); };
                            Task task = new Task(action);
                            writeTasks.Add(task);
                            task.Start();
                        }
                        Task.WaitAll(writeTasks.ToArray());


                        Console.WriteLine("UNLOCKING WRITES");
                    }


                    // Write success
                    return (true, "-1");
  
                }
                else
                {
                    // write failed, but we can redirect the client to correct server
                    return (false, Partitions[partitionID].MasterServerID);
                }

            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            
            return (false, "-1");


        }


        public void LockObject(string partitionID, string objectID, int writeID)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            Console.WriteLine("HERE");
            foreach (KeyValuePair<string, Partition> kvp in Partitions)
                Console.WriteLine(kvp.Key);
            try
            {
                Partitions[partitionID].LockValue(objectID, writeID);
            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            Console.WriteLine("Hnow hERE");

        }


        public void WriteObject(string partitionID, string objectID, string value, int writeID)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            Partitions[partitionID].AddKeyPair(objectID, value, writeID);
            Partitions[partitionID].UnlockValue(objectID);
        }


        private void CheckFreezeLock()
        {

            lock (FreezeLock)
                while (Frozen)
                    Monitor.Wait(FreezeLock);

        }


        public void Freeze()
        {
            SimulateCommunicationDelay();

            lock (FreezeLock)
                Frozen = true;
        }


        public void Unfreeze()
        {
            SimulateCommunicationDelay();

            lock (FreezeLock)
            {
                Frozen = false;
                Monitor.PulseAll(FreezeLock);
            }
        }


        public void InitPartition(string pid, string sid, List<string> servers)
        {
            CheckFreezeLock();

            Partition partition = new Partition(sid, servers);
            Partitions.TryAdd(pid, partition);
        }


        public void Replication(int rfactor) { 
            
            foreach (KeyValuePair<string, Partition> kvp in Partitions)
                if (kvp.Value.MasterServerID.Equals(ServerID))
                {
                    foreach (string serverID in kvp.Value.AssociatedServers)
                    {
                        SetPartitionRequest request = new SetPartitionRequest {
                            PartitionID = kvp.Key,
                            MainServerID = kvp.Value.MasterServerID,
                        };
                        request.AssociatedServers.Add(kvp.Value.AssociatedServers);

                        NodesCommunicator.GetServerClient(serverID).SetPartitionAsync(request);
                    }
                }
            
        }


        public void NewPartition(string pid, List<string> servers)
        {
            CheckFreezeLock();

            Partition partition = new Partition(ServerID, servers);

            if (!Partitions.TryAdd(pid, partition))
                Partitions[pid] = partition;

            // Inits Partition on Replicates
            //foreach (string serverID in servers)
            //{
            //    SetPartitionRequest request = new SetPartitionRequest
            //    {
            //        PartitionID = pid,
            //        MainServerID = ServerID
            //    };
            //    request.AssociatedServers.Add(servers);
            //    NodesCommunicator.GetServerClient(serverID).SetPartition(request);
            //}
        }

        public void PrintStatus()
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();


            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                Console.WriteLine("===Partition " + part.Key + " With Master Server " + part.Value.MasterServerID + " ===");
                foreach (KeyValuePair<string, string> kvp in part.Value.Storage)
                    Console.WriteLine("ObjectID = {0}, Value = {1}", kvp.Key, kvp.Value);
            }
        }


        public List<string> ListServerPartitions()
        {
            CheckFreezeLock();

            List<string> listing = new List<string>();

            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                string partitionListing =  "=== Partition " + part.Key + " with master server " + part.Value.MasterServerID + " ===\n";
                foreach (KeyValuePair<string, string> kvp in part.Value.Storage)
                    partitionListing += "ObjectID = " + kvp.Key + " Value = " + kvp.Value + "\n";
                listing.Add(partitionListing);
            }

            return listing;
        }


        private void SimulateCommunicationDelay()
        {
            if (Delay > 0)
                Thread.Sleep(Delay);
        }

    }
}
