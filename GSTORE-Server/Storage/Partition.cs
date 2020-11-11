using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace GSTORE_Server.Storage
{
    class Partition
    { 
        public string MasterServerID { get; }
        public List<String> AssociatedServers = new List<String>();

        public readonly ConcurrentDictionary<string, string> Storage = new ConcurrentDictionary<string, string>();
        public readonly ConcurrentDictionary<string, int> StorageLockers = new ConcurrentDictionary<string, int>();

        public Partition(string sid, List<String> servers)
        {
            MasterServerID = sid;
            AssociatedServers = servers;
        }

        public void AddKeyPair(string objectID, string value, int writeID)
        {
            Storage[objectID] = value;
            UnlockValue(objectID);
        }

        public (bool, string) GetValue(string objectID)
        {
            do { Thread.Sleep(500); } while (!(StorageLockers[objectID] == -1)) ;

            if (Storage.TryGetValue(objectID, out global::System.String value))
                return (true, value);

            return (false, "N/A");
        }

        public void LockValue(string objectID, int writeID)
        {
            StorageLockers.AddOrUpdate(objectID, writeID, (key, oldvalue) => oldvalue = writeID);

            if (!Storage.ContainsKey(objectID))
                Storage.TryAdd(objectID, null);
        }

        public void UnlockValue(string objectID)
        {
            StorageLockers.AddOrUpdate(objectID, -1, (key, oldvalue) => oldvalue = -1);
        }
    }
}
