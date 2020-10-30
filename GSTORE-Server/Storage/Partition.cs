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

        public readonly ConcurrentDictionary<string, string> Storage = new ConcurrentDictionary<string, string>();
        public readonly ConcurrentDictionary<string, Semaphore> StorageLockers = new ConcurrentDictionary<string, Semaphore>();

        public List<String> AssociatedServers = new List<String>();


        public Partition(string sid, List<String> servers)
        {
            MasterServerID = sid;
            AssociatedServers = servers;
            AssociatedServers.Add(sid);
        }

        public void AddKeyPair(string objectID, string value)
        {
            Console.WriteLine("alo");
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
            if (!Storage.ContainsKey(objectID))
            {
                Storage.TryAdd(objectID, null);
                StorageLockers.TryAdd(objectID, new Semaphore(0, 1));
            }

            StorageLockers[objectID].WaitOne();
        }

        public void UnlockValue(string objectID)
        {
            StorageLockers[objectID].Release();
        }
    }
}
