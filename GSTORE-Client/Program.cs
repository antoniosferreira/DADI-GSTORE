using System;
using System.Runtime.CompilerServices;
using System.IO;

namespace GSTORE_Client
{
    class Program
    {

        static void Main(string[] args)
        {
            bool run = true;
            string clientID, clientURL, clientScript = "";
            if (args.Length < 2)
            {
                Console.WriteLine("Failed to initiate the client");
                Console.WriteLine("Usage: <clientID> <clientURL> <clientScript?>");

                Console.Write("clientID:");
                clientID = Console.ReadLine();
                Console.Write("clientURL:");
                clientURL = Console.ReadLine();
            
            } else 
            {
                clientID = args[0];
                clientURL = args[1];

                if (args.Length == 3)
                    clientScript = args[2];
            }

            try
            {
                GSClient client = new GSClient(clientID, clientURL);
                Console.WriteLine("=== CLIENT RUNNING ===");
                Console.WriteLine("ClientID:" + clientID + " ClientURL:" + clientURL);


                // Executes first script, if exists
                if (!clientScript.Equals(""))
                {
                    string scriptPath = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\NodesConfigurator\\";
                    clientScript = scriptPath + clientScript + ".txt";

                    string[] commands = System.IO.File.ReadAllLines(clientScript);
                    foreach (string command in commands)
                    {
                        Console.WriteLine("{0}", command);
                        client.ParseCommand(command);
                    }
                }

                // Parses and Executes Commands
                do
                {
                    Console.Write("-->");
                    run = client.ParseCommand(Console.ReadLine());
                } while (run);

            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            } 
        }
    }
}
