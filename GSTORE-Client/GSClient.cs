using System;
using System.Collections.Generic;
using Grpc.Net.Client;
using GSTORE_Client.Commands;
using NodesConfigurator;

namespace GSTORE_Client
{
    class GSClient
    {
        // Client Configuration Information
        public string ClientID { get { return _clientID; } }
        private readonly string _clientID;
        public string ClientURL { get { return _clientURL; } }
        private readonly string _clientURL;


        private List<Command> Commands;

        public string CurrentServer = null;
        public NodesCommunicator NodesCommunicator = new NodesCommunicator();

        public GSClient(string clientID, string clientURL)
        {
            _clientID = clientID;
            _clientURL = clientURL;

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
            Console.WriteLine("ERROR: Invalid command \"{0}\"", command);
            return true;
        }
    }
}
