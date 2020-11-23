using System;
using System.Collections.Generic;
using System.Text;

namespace GSTORE_Server.Storage
{
    public class WriteData
    {
        public int Tid { get; }
        public string Pid { get; }
        public string Oid { get; }
        public string Value { get; }


        public WriteData(int tid, string pid, string oid, string value)
        {
            Tid = tid;
            Pid = pid;
            Oid = oid;
            Value = value;
        }

        public WriteData(WriteRequestData request)
        {
            Tid = request.Tid;
            Pid = request.Pid;
            Oid = request.Oid;
            Value = request.Value;
        }

        public WriteData(WriteRequest request)
        {
            Tid = -1;
            Pid = request.PartitionID;
            Oid = request.ObjectID;
            Value = request.Value;
        }

        public WriteRequestData ToRequest()
        {
            return new WriteRequestData
            {
                Tid = Tid,
                Oid = Oid,
                Pid = Pid,
                Value = Value
            };
        }
    }
}
