using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;



namespace GSTORE_Server.Storage
{
    class Partition
    { 
        public string PartitionLeader { get; set; }
        public List<String> ReplicatedServers = new List<String>();
        public readonly ConcurrentDictionary<int, WriteData> Buffer = new ConcurrentDictionary<int, WriteData>();

        public readonly ConcurrentDictionary<string, string> Items = new ConcurrentDictionary<string, string>();

        public Partition(string sid, List<String> servers)
        {
            PartitionLeader = sid;
            ReplicatedServers = servers;
        }

        public void AddItem(WriteData write)
        {
            Buffer.AddOrUpdate(write.Tid, write, (k, v) => v = write);
            Items.AddOrUpdate(write.Oid, write.Value, (k, v) => v = write.Value);
        }

        public string GetValue(string objectID)
        {
            if (Items.TryGetValue(objectID, out string value))
                return value;
            
            return "N/A";
        }
    }
}
