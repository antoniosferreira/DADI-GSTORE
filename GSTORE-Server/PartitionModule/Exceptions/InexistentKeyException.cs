using System;
using System.Collections.Generic;
using System.Text;

namespace GSTORE_Server.PartitionModule.Exceptions
{
    [Serializable]
    public class InexistentKeyException : Exception
    {
        public InexistentKeyException(string message)
        : base(message) { }
    }
}
