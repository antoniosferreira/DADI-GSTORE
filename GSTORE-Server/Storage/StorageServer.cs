using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GSTORE_Server.Storage
{
    public class StorageServer
    {
        public string ServerID { get; }
        public int Delay { get; }


        // Partitions that this server stores
        private ConcurrentDictionary<string, Partition> Partitions = new ConcurrentDictionary<string, Partition>();
        private NodesCommunicator NodesCommunicator = new NodesCommunicator();


        readonly object WriteLock = new object();
        private bool WriteLocked = false;

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
            if (!Partitions.ContainsKey(partitionID.ToUpper()))
                return (false, "-1");


            return Partitions[partitionID].GetValue(objectID);
        }


        public (bool, string) Write(string partitionID, string objectID, string value)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();


            if (!Partitions.ContainsKey(partitionID))
                return (false, "-1");

            if (Partitions[partitionID].MasterServerID.Equals(ServerID))
            {

                // LOCKS FURTHER WRITES
                lock (WriteLock)
                    WriteLocked = true;


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
                    Action action = () => { LockObjectReply reply = NodesCommunicator.GetServerClient(serverID).LockObject(lockRequest); };
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


                // UNLOCKS FURTHER WRITES
                lock (WriteLock)
                {
                    WriteLocked = false;
                    Monitor.PulseAll(WriteLock);
                }

                // Write success
                return (true, "-1");
            } else
            {
                // write failed, but we can redirect the client to correct server
                return (false, Partitions[partitionID].MasterServerID);
            }

        }

        public void LockObject(string partitionID, string objectID, int writeID)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            Partitions[partitionID].LockValue(objectID, writeID);
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
            SimulateCommunicationDelay();

            Partition partition = new Partition(sid, servers);
            Partitions.TryAdd(pid, partition);
        }


        public void NewPartition(string pid, List<string> servers)
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            Partition partition = new Partition(ServerID, servers);
            Partitions.TryAdd(pid, partition);

            // Inits Partition on Replicates
            foreach (string serverID in servers)
            {
                SetPartitionRequest request = new SetPartitionRequest
                {
                    PartitionID = pid,
                    MainServerID = ServerID
                };

                request.AssociatedServers.Add(servers);
                NodesCommunicator.GetServerClient(serverID).SetPartition(request);
            }
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

        public string ListMasterPartitions() 
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            string listing = "";
           
            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                if (part.Value.MasterServerID.Equals(ServerID)) { 
                    listing += "=== Partition " + part.Key + " with master server " + part.Value.MasterServerID + " ===\n";
                    foreach (KeyValuePair<string, string> kvp in part.Value.Storage)
                        listing += "ObjectID = " + kvp.Key + " Value = " + kvp.Value + "\n";
                }
            }

            return listing;
        }


        public string ListServerPartitions()
        {
            CheckFreezeLock();
            SimulateCommunicationDelay();

            string listing = "";

            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                listing += "=== Partition " + part.Key + " with master server " + part.Value.MasterServerID + " ===\n";
                foreach (KeyValuePair<string, string> kvp in part.Value.Storage)
                    listing += "ObjectID = " + kvp.Key + " Value = " + kvp.Value + "\n";
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
