using System;
using System.Reflection.Metadata.Ecma335;
using Google.Protobuf;
using Grpc.Net.Client;
using System.IO;

namespace PM
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Failed to initiate the Puppet Master");
                Console.WriteLine("Usage: <nodesFile> <pmScript>?");

                Environment.Exit(1);
            }


            PM puppet = new PM();
            Console.WriteLine("==== PM RUNNING ====");
            Console.WriteLine("# Enter exit to leave");


            // Executes PM Script if existent
            if (args.Length == 2)
            {
                string scriptPath = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\NodesConfigurator\\";
                var pmScript = scriptPath + args[1] + ".txt";

                    string[] commands = System.IO.File.ReadAllLines(pmScript);
                    foreach (string command in commands)
                    {
                        Console.WriteLine("{0}", command);
                        puppet.ParseCommand(command);
                    }
            }


            bool Run;
            do
            {
                Console.Write("$>");
                Run = puppet.ParseCommand(Console.ReadLine());
            } while (Run);
        }
    }
    
}
