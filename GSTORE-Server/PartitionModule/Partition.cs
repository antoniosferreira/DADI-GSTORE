using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GSTORE_Server.PartitionModule.Exceptions;


namespace GSTORE_Server.PartitionModule
{
    public class Partition
    {
        public string PartitionID { get { return _partitionID; } }
        private readonly string _partitionID;

        private readonly ConcurrentDictionary<string, string> Storage;



        public Partition(string id)
        {
            _partitionID = id;
            Storage = new ConcurrentDictionary<string, string>();
        }

        public string WriteValue(string key, string value)
        {

            if (Storage.TryAdd(key, value) == false)
            {
                Storage[key] = value;
            }

            return Storage[key];
        }

        public string ReadValue(string key)
        {
            string result;

            if (Storage.TryGetValue(key, out result))
            {
                return result;
            }
            else
            {
                throw new InexistentKeyException("The key " + key + " is not registered");
            }
        }


        public bool ContainsKey(string key)
        {
            Console.WriteLine(Storage[key]);
            return Storage.ContainsKey(key);
        }

        public string Print()
        {
            string text = "";
            foreach (KeyValuePair<string, string> kvp in Storage)
            {
                text += string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }
            return text;
        }

    }
}
