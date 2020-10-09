using System;
using System.Collections.Generic;
using Grpc.Net.Client;
using PM.Commands;

namespace PM
{
    class PM
    {
        public const string PCSUrl = "http://localhost:10000";
        public PCSServices.PCSServicesClient PCSClient;
        
        
        private List<Command> Commands;


        public PM()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress(PCSUrl);
            PCSClient = new PCSServices.PCSServicesClient(channel);

            Commands = new List<Command>
            {
                new Server(PCSClient),
                new Client(PCSClient)
                
            };
        }


        public bool ParseCommand(string command)
        {
            foreach (Command cmd in Commands)
            {
                if (cmd.Check(command))
                {
                    cmd.Exec(command);
                    //return cmd.GetType() != typeof(Exit);
                    return true;
                }
            }

            Console.WriteLine("ERROR: Invalid command \"{0}\"", command);
            return true;
        }


    }
}
