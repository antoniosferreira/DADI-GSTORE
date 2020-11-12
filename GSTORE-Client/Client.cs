using System;
using System.Collections.Generic;
using GSTORE_Client.Commands;
using GSTORE_Client.Communication;

namespace GSTORE_Client
{
    class Client
    {
        // Client Configuration Information
        public string ClientID { get; }
        public string ClientURL { get; }


        // Client Communication Data
        public string CurrentServer = null;
        public NodesCommunicator NodesCommunicator = new NodesCommunicator();

        private readonly List<Command> Commands;

        public Client(string clientID, string clientURL)
        {
            ClientID = clientID;
            ClientURL = clientURL;
            CurrentServer = null;

            Commands = new List<Command>
            {
                new Read(this),
                new Write(this),
                new ListServer(this),
                new ListGlobal(this),
                new Wait(),
                new Exit()
            };

        }

        public bool ParseCommand(string command)
        {
            foreach (Command cmd in Commands)
                if (cmd.Check(command))
                {
                    cmd.Exec(command);
                    return true;
                }
            Console.WriteLine(">>> ERROR: Invalid command \"{0}\"", command);
            return true;
        }

        public void PrintStatus()
        {
            string server = CurrentServer ?? "none";
            Console.WriteLine(">>> Current Attached Server: " + server);
        }
    }
}
