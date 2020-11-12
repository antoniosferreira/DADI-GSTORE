using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GSTORE_Server.Communication
{
    class Sequentiator
    {

        class TID
        {
            private int _Tid = 0;

            public TID(int tid) { _Tid = tid; }

            public int GetNewTID()
            {
                return Interlocked.Increment(ref _Tid);
            }

            public int GetTID()
            {
                return _Tid;
            }
        }


        private readonly ConcurrentDictionary<string, TID> Objects = new ConcurrentDictionary<string, TID>();

        public Sequentiator() {}

        public int GetTID(string objectID)
        {            
            if (Objects.ContainsKey(objectID))
                return Objects[objectID].GetNewTID();

            return Objects.AddOrUpdate(objectID, new TID(0), (k, v) => new TID(0)).GetNewTID();
        }

        public int GetCurrentTID(string objectID)
        {
            return Objects[objectID].GetTID();
        }

        public void UpdateTID(string objectID, int tid)
        {
            Objects.AddOrUpdate(objectID, new TID(tid), (k, v) => new TID(tid));
        }

        public string ListSequentiator()
        {
            string listing = "";
            foreach (KeyValuePair<string, TID> kvp in Objects)
                listing += kvp.Key + "---" + kvp.Value.GetTID() + "\n";

            return listing;
        }
    }
}
