using System;

namespace GSTORE_Client
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Failed to initiate the client");
                Console.WriteLine("Usage: <clientID> <clientURL> <clientScript>");

                Environment.Exit(1);
            }

            try
            {
                GSClient client = new GSClient(args[0], args[1], args[2]);
                Console.WriteLine("=== CLIENT RUNNING ===");
                Console.WriteLine("ClientID:" + args[0] + " ClientURL:" + args[1]);

                client.Start();
                Console.ReadLine();

            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            } 
        }
    }
}
