using System;
using System.Collections.Generic;
using System.Text;

namespace GSTORE_Server.Storage
{
    class Item
    {
        private readonly string Value;
        private readonly int TID;

        public Item(string value, int tid)
        {
            Value = value;
            TID = tid;
        }

        public string GetValue()
        {
            return Value;
        }

        public int GetTID()
        {
            return TID;
        }
    }
}
