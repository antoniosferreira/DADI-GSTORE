using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GSTORE_Server.Storage
{
    class StorageServer
    {
        public string ServerID { get; }

        // Partitions that this server is master of
        private List<Partition> Partitions = new List<Partition>();
        private NodesCommunicator NodesCommunicator = new NodesCommunicator();

        readonly object WriteLock = new object();
        private bool WriteLocked = false;

        readonly object FreezeLock = new object();
        private bool Frozen = false;


        public StorageServer(string serverID) {
            ServerID = serverID;
        }


        // GSTORE-CLIENT OPERATIONS
        public (bool, string) Read(string partitionID, string objectID)
        {
            CheckFreezeLock();

            foreach (Partition partition in Partitions)
                if (partition.PartitionID.Equals(partitionID))
                    return partition.GetValue(objectID);

            return (false, "N/A");
        }


        public (bool, string) Write(string partitionID, string objectID, string value)
        {
            CheckFreezeLock();

            foreach (Partition partition in Partitions)
                if (partition.PartitionID.Equals(partitionID)) {
                    if (partition.MasterServerID.Equals(ServerID)) {
                        
                        // LOCKS FURTHER WRITES
                        lock (WriteLock)
                            WriteLocked = true;

                        LockObjectRequest lockRequest = new LockObjectRequest
                        {
                            PartitionID = partitionID,
                            ObjectID = objectID
                        };

                        // Sends Lock to Every Server
                        List<Task> requestTasks = new List<Task>();
                        foreach (string serverID in partition.AssociatedServers)
                        {
                            Action action = () => {LockObjectReply reply = NodesCommunicator.GetServerClient(serverID).LockObject(lockRequest);};
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
                            Value = value
                        };

                        List<Task> writeTasks = new List<Task>();
                        foreach (string serverID in partition.AssociatedServers)
                        {
                            Action action = () => { WriteObjectReply reply = NodesCommunicator.GetServerClient(serverID).WriteObject(writeRequest); };
                            Task task = new Task(action);
                            writeTasks.Add(task);
                            task.Start();
                        }
                        Task.WaitAll(requestTasks.ToArray());


                        // UNLOCKS FURTHER WRITES
                        lock (WriteLock)
                        {
                            WriteLocked = false;
                            Monitor.PulseAll(WriteLock);
                        }

                    }

                    return (false, partition.MasterServerID);
                }

            return (false, "-1");
        }

        public void WriteObject(string partitionID, string objectID, string value)
        {
            foreach (Partition partition in Partitions)
                if (partition.PartitionID.Equals(partitionID))
                {
                    partition.AddKeyPair(objectID, value);
                    partition.UnlockValue(objectID);
                }
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


        public void LockObject(string partitionID, string objectID)
        {
            foreach (Partition partition in Partitions)
            {
                if (partition.PartitionID.Equals(partitionID))
                {
                    partition.LockValue(objectID);
                }
            }
        }



        private void CheckFreezeLock()
        {
            lock (FreezeLock)
                while (Frozen) 
                    Monitor.Wait(FreezeLock);
            
        }

        public void PrintStatus()
        {
            Console.WriteLine("PRINT STATUS");
        }

    }
}
