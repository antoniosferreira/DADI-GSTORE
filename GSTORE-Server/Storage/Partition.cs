using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using System.Text;

namespace GSTORE_Server.Storage
{
    class Partition
    {
        public string PartitionID { get; }
        private readonly ConcurrentDictionary<string, string> Storage = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, Semaphore> StorageLockers = new ConcurrentDictionary<string, Semaphore>();

        public List<String> AssociatedServers = new List<String>();


        public string MasterServerID { get; }
    

        public Partition(string id, string sid)
        {
            PartitionID = id;
            MasterServerID = sid;
        }

        public void AddKeyPair(string objectID, string value)
        {
            if (Storage.TryAdd(objectID, value))
            {
                StorageLockers.TryAdd(objectID, new Semaphore(0,1));
                return;
            }
            
            Storage[objectID] = value;
        }

        public (bool, string) GetValue(string objectID)
        {
            if (Storage.TryGetValue(objectID, out global::System.String value))
                return (true, value);

            return (false, "N/A");
        }

        public void LockValue(string objectID)
        {
            StorageLockers[objectID].WaitOne();
        }

        public void UnlockValue(string objectID)
        {
            StorageLockers[objectID].Release();
        }
    }
}
