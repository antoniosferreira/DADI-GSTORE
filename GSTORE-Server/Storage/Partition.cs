using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using System.Text;
using System.ComponentModel.DataAnnotations;

namespace GSTORE_Server.Storage
{
    class Partition
    { 
        public string MasterServerID { get; }
        // Servers whom replicate this partition
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
            do { Thread.Sleep(500); } while (!(StorageLockers[objectID] == writeID));
            
            // ObjectID is inserted when lock
            Storage[objectID] = value;
        }

        public (bool, string) GetValue(string objectID)
        {
            do { Thread.Sleep(500); } while (!(StorageLockers[objectID] == -1)) ;

            Console.WriteLine("pois");

            if (Storage.TryGetValue(objectID, out global::System.String value))
                return (true, value);

            return (false, "N/A");
        }

        public void LockValue(string objectID, int writeID)
        {
            // Locks the object
            StorageLockers.AddOrUpdate(objectID, writeID, (key, oldvalue) => writeID);

            if (!Storage.ContainsKey(objectID))
                Storage.TryAdd(objectID, null);
        }

        public void UnlockValue(string objectID)
        {
            StorageLockers.AddOrUpdate(objectID, -1, (key, oldvalue) => -1);
        }
    }
}
