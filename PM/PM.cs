using System;
using System.Collections.Generic;
using Grpc.Net.Client;
using PM.Commands;
using NodesConfigurator;

namespace PM
{
    class PM
    {
        public PCSServices.PCSServicesClient PCSClient;
        public const string PCSUrl = "http://localhost:10000";
      

        private List<Command> Commands;
        public NodesCommunicator NodesCommunicator = new NodesCommunicator();

        public PM()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress(PCSUrl);
            PCSClient = new PCSServices.PCSServicesClient(channel);

            Commands = new List<Command>
            {
                new Server(PCSClient, this),
                new Client(PCSClient),
                new ReplicationFactor(this),
                new Partition(this),
                new Crash(this),
                new Unfreeze(this),
                new Freeze(this),
                new Status(this),
                new Wait(),
                new Exit()

            };
        }


        public bool ParseCommand(string command)
        {
            foreach (Command cmd in Commands)
            {
                if (cmd.Check(command))
                {
                    cmd.Exec(command);
                    return true;
                }
            }

            Console.WriteLine("ERROR: Invalid command \"{0}\"", command);
            return true;
        }


    }
}
