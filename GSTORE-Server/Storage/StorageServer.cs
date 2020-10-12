using System;
using System.Threading;

namespace GSTORE_Server.Storage
{
    class StorageServer
    {
        readonly object FreezeLock = new object();
        private bool Frozen = false;


        public StorageServer() { }

        public string Read(string key)
        {
            CheckFreezeLock();
            return "Read " + key;
        }

        public bool Write(string key, string value)
        {
            CheckFreezeLock();
            return true;
        }


        public void Freeze()
        {
            lock (FreezeLock)
                Frozen = true;
            
        }

        public void Unfreeze()
        {
            lock (FreezeLock)
            {
                Frozen = false;
                Monitor.PulseAll(FreezeLock);
            }
        }

        private void CheckFreezeLock()
        {
            lock (FreezeLock)
                while (Frozen) 
                    Monitor.Wait(FreezeLock);
            
        }

        public void PrintStatus()
        {
            Console.WriteLine("PRINT STATUS");
        }

    }
}
