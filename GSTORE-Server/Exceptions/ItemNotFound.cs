using System;
using System.Collections.Generic;
using System.Text;

namespace GSTORE_Server.Exceptions
{
    class ItemNotFound : Exception
    {
        public ItemNotFound() : base() { }
        public ItemNotFound(string message) : base(message) { }
    }
}
