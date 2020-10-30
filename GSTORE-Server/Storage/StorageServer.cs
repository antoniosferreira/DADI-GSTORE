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

        // Partitions that this server is master of
        private ConcurrentDictionary<string, Partition> Partitions = new ConcurrentDictionary<string, Partition>();
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

            Partitions[partitionID].GetValue(objectID);

            return (false, "N/A");
        }


        public (bool, string) Write(string partitionID, string objectID, string value)
        {
            CheckFreezeLock();

            if (Partitions[partitionID].MasterServerID.Equals(ServerID))
            {

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
                foreach (string serverID in Partitions[partitionID].AssociatedServers)
                {
                    Action action = () => { LockObjectReply reply = NodesCommunicator.GetServerClient(serverID).LockObject(lockRequest); Console.WriteLine(reply); };
                    Task task = new Task(action);
                    requestTasks.Add(task);
                    task.Start();

                }
                Task.WaitAll(requestTasks.ToArray());

                Console.WriteLine("debug to write");
                // Sends Write to Every Server
                WriteObjectRequest writeRequest = new WriteObjectRequest
                {
                    PartitionID = partitionID,
                    ObjectID = objectID,
                    Value = value
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

                Console.WriteLine("all");

                // UNLOCKS FURTHER WRITES
                lock (WriteLock)
                {
                    WriteLocked = false;
                    Monitor.PulseAll(WriteLock);
                }

            } else
            {
                return (false, Partitions[partitionID].MasterServerID);
            }

            return (false, "-1");
        }

        public void WriteObject(string partitionID, string objectID, string value)
        {
            Partitions[partitionID].AddKeyPair(objectID, value);
            Partitions[partitionID].UnlockValue(objectID);
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
            Partitions[partitionID].LockValue(objectID);
        }


        public void InitPartition(string pid, string sid, List<string> servers)
        {
            Partition partition = new Partition(sid, servers);
            Partitions.TryAdd(pid, partition);
        }

        public void NewPartition(string pid, List<string> servers)
        {
            Partition partition = new Partition(ServerID, servers);
            Partitions.TryAdd(pid, partition);

            foreach(string serverID in servers)
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

        private void CheckFreezeLock()
        {
            lock (FreezeLock)
                while (Frozen) 
                    Monitor.Wait(FreezeLock);
            
        }

        public void PrintStatus()
        {
            Console.WriteLine("Status received");
            foreach (KeyValuePair<string, Partition> part in Partitions)
            {
                Console.WriteLine("===Partition " + part.Key + " With Master Server " + part.Value.MasterServerID + " ===");
                foreach (KeyValuePair<string, string> kvp in part.Value.Storage)
                    Console.WriteLine("ObjectID = {0}, Value = {1}", kvp.Key, kvp.Value);
            }
        }

    }
}
