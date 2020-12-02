using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GSTORE_Server.Communication;


namespace GSTORE_Server.Storage
{
    class Partition
    {
        public string PartitionID { get; }
        public View PreviousView { get; set; } = null;
        public View CurrentView { get; set; }


        public readonly ConcurrentDictionary<string, (int, string)> Items = new ConcurrentDictionary<string, (int, string)>();
        public int Sequencer = 0;


        public Partition(string pid, List<String> servers)
        {
            PartitionID = pid;
            Sequencer = 0;
            CurrentView = new View(0, servers[0], servers);
        }

        public void AddItem(WriteData write)
        {
            CurrentView.Buffer.AddOrUpdate(write.Tid, write, (k, v) => v = write);
            
            // New object written
            if (!Items.ContainsKey(write.Oid))
            {
                Items.AddOrUpdate(write.Oid, (write.Tid, write.Value), (k, v) => v = (write.Tid, write.Value));
                if (write.Tid > Sequencer)
                    Sequencer = write.Tid;

                return;
            }

            // Update on a given object
            if (write.Tid > Items[write.Oid].Item1)
            {
                Items.AddOrUpdate(write.Oid, (write.Tid, write.Value), (k, v) => v = (write.Tid, write.Value));
                if (write.Tid > Sequencer)
                    Sequencer = write.Tid;
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
