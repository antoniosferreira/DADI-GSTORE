using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GSTORE_Server.Communication;


namespace GSTORE_Server.Storage
{
    class Partition
    {         
        public readonly ConcurrentDictionary<string, (int, string)> Items = new ConcurrentDictionary<string, (int, string)>();
        public readonly ConcurrentDictionary<string, int> ReplicasSequencers = new ConcurrentDictionary<string, int>();

        public string PartitionID { get; }

        public View PreviousView { get; set; } = null;
        public View CurrentView { get; set; }

        public Partition(string pid, List<String> servers)
        {
            PartitionID = pid;
            CurrentView = new View(0, servers[0], servers);

            foreach (string server in CurrentView.ViewParticipants) 
                ReplicasSequencers.AddOrUpdate(server, 0, (k,v)=>v=0);
            
        }

        public void AddItem(WriteData write)
        {
            CurrentView.Buffer.AddOrUpdate(write.Tid, write, (k, v) => v = write);
            
            if (!Items.ContainsKey(write.Oid))
            {
                Items.AddOrUpdate(write.Oid, (write.Tid, write.Value), (k, v) => v = (write.Tid, write.Value));
                if (write.Tid > ReplicasSequencers[CurrentView.ViewLeader])
                    ReplicasSequencers[CurrentView.ViewLeader] = write.Tid;

                return;
            }

            if (write.Tid > Items[write.Oid].Item1)
            {
                Items.AddOrUpdate(write.Oid, (write.Tid, write.Value), (k, v) => v = (write.Tid, write.Value));
                if (write.Tid > ReplicasSequencers[CurrentView.ViewLeader])
                    ReplicasSequencers[CurrentView.ViewLeader] = write.Tid;
            }
        }

        public string GetValue(string objectID)
        {
            if (Items.TryGetValue(objectID, out (int, string) value))
                return value.Item2;
            
            return "N/A";
        }
    }
}
