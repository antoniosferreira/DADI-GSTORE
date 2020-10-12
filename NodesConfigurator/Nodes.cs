using System;
using System.Collections.Generic;
using System.IO;

namespace NodesConfigurator
{
    public class Nodes
    {
        private Dictionary<string, string> ClientsList = new Dictionary<string, string>();
        private Dictionary<string, string> ServersList = new Dictionary<string, string>();

        public Nodes()
        {
            string line;
            string[] words;

            System.IO.StreamReader file = new System.IO.StreamReader(FindFilePath());
            while ((line = file.ReadLine()) != null)
            {
                words = line.Split(null);
                if (words.Length < 3) { continue; }

                if (words[0].Equals("SERVER"))
                    ServersList.Add(words[1], words[2]);
                if (words[0].Equals("CLIENT"))
                    ClientsList.Add(words[1], words[2]);
            }

            foreach (KeyValuePair<string, string> kvp in ClientsList)
            {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }

            foreach (KeyValuePair<string, string> kvp in ServersList)
            {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }

        }

        public Dictionary<string, string> GetAllServers()
        {
            return ServersList;
        }

        public Dictionary<string, string> GetAllClients()
        {
            return ClientsList;
        }

        public string GetClientAddress(string id)
        {
            return ClientsList[id];
        }

        public string GetServerAddress(string id)
        {
            return ServersList[id];
        }

        public string FindFilePath()
        {
            string nodesPath = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\NodesConfigurator\\nodes.txt";
            return nodesPath;
        }


    }
}
