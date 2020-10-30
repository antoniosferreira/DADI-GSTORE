using System;

namespace GSTORE_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // EVALUATES SERVER INITIAL CONFIGURATION
            if (args.Length < 4)
            {
                Console.WriteLine("Failed to initiate the server");
                Console.WriteLine("Usage: <serverID> <serverURL> <minDelay> <maxDelay>");

                Environment.Exit(1);
            }

            try
            {
                var uri = new Uri(args[1]);

                string ServerID = args[0];

                string ServerURL = uri.Host;
                int ServerPort = uri.Port; ;
                int MinDelay = int.Parse(args[2]);
                int MaxDelay = int.Parse(args[3]);

                GSServer server = new GSServer(ServerID, ServerURL, ServerPort, MinDelay, MaxDelay);
                server.Start();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
