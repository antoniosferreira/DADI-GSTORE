using System;
using System.Reflection.Metadata.Ecma335;
using Google.Protobuf;
using Grpc.Net.Client;

namespace PM
{
    class Program
    {

        static void Main(string[] args)
        {
            bool Run = true;


            if (args.Length < 1)
            {
                Console.WriteLine("Failed to initiate the Puppet Master");
                Console.WriteLine("Usage: <nodesFile> <clientScripts>");

                Environment.Exit(1);
           }

            PM puppet = new PM();
            Console.WriteLine("==== PM RUNNING ====");

            do
            {
                Console.Write("-->");
                Run = puppet.ParseCommand(Console.ReadLine());
            } while (Run);
        }
    }
    
}
