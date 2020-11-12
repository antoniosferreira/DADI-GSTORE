using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using GSTORE_Server.Exceptions;

namespace GSTORE_Server.Storage
{
    class Partition
    { 
        public string MasterServerID { get; }
        public List<String> AssociatedServers = new List<String>();

        public readonly ConcurrentDictionary<string, Item> Items = new ConcurrentDictionary<string, Item>();
        public readonly ConcurrentDictionary<string, bool> ItemLockers = new ConcurrentDictionary<string, bool>();

        public Partition(string sid, List<String> servers)
        {
            MasterServerID = sid;
            AssociatedServers = servers;
        }

        public void AddItem(string objectID, string value, int TID)
        {
            Item item = new Item(value, TID);
            Items.AddOrUpdate(objectID, item, (key, value) => item);
            UnlockValue(objectID);
        }

        public (string, int) GetValue(string objectID)
        {
            if (ItemLockers.ContainsKey(objectID))
            {
                do { Thread.Sleep(500); } while (ItemLockers[objectID]);

                if (Items.TryGetValue(objectID, out Item item))
                    return (item.GetValue(), item.GetTID());
            }

            return ("N/A", -1);
        }

        public void LockValue(string objectID)
        {
            ItemLockers.AddOrUpdate(objectID, true, (key, oldvalue) => oldvalue = true);

            if (!Items.ContainsKey(objectID))
                Items.TryAdd(objectID, null);
        }

        public void UnlockValue(string objectID)
        {
            ItemLockers.AddOrUpdate(objectID, false, (key, oldvalue) => oldvalue = false);
        }
    }
}
