using System;
using System.Runtime.CompilerServices;
using System.IO;
using Grpc.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            
            } 
            else 
            {
                clientID = args[0];
                clientURL = args[1];

                if (args.Length == 3)
                    clientScript = args[2];
            }

            try
            {
                Client client = new Client(clientID, clientURL);
                Console.WriteLine("=== CLIENT RUNNING ===");
                Console.WriteLine("ClientID:" + clientID + "\nClientURL:" + clientURL);

                // Inits Services
                Uri uri = new Uri(clientURL);                
                Grpc.Core.Server server = new Grpc.Core.Server
                {
                    Services = {StatusService.BindService(new StatusServices(client))},
                    Ports = { new ServerPort(uri.Host, uri.Port, ServerCredentials.Insecure) }
                };
                server.Start();

                Console.WriteLine("Press any key to stop...");


                List<string> repeatInstruction = new List<string> { };
                bool cycle = false;
                int cycles = 0;

                // Firstly executes script, if provided
                if (!clientScript.Equals(""))
                {
                    string scriptPath = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\NodesConfigurator\\";
                    clientScript = scriptPath + clientScript + ".txt";

                    
                    string[] commands = System.IO.File.ReadAllLines(clientScript);
                    foreach (string command in commands)
                    {
                        if (command.Contains("begin-repeat")) {
                            repeatInstruction.Clear();

                            cycles = Int32.Parse(Regex.Match(command, @"\d+").Value);
                            cycle = true;
                        }

                        else if (command.Contains("end-repeat")) {


                            for (int i =0; i<cycles; i++)
                            {
                                List<string> finalCommand = new List<string> { };
                                foreach (string s in repeatInstruction)
                                    finalCommand.Add(s.Replace("$i", "" + i));
                                
                                foreach (string s in finalCommand)
                                {
                                    Console.WriteLine("{0}", s);
                                    client.ParseCommand(s);
                                }
                            }
                            
                            cycle = false;
                            
                        } else
                        {
                            if (cycle) repeatInstruction.Add(command);
                            else
                            {
                                Console.WriteLine("{0}", command);
                                client.ParseCommand(command);
                            }
                        } 
                    }
                }

                do
                {
                    Console.Write(">");

                    string command = Console.ReadLine();
                    
                    if (command.Contains("begin-repeat"))
                    {
                        repeatInstruction.Clear();

                        cycles = Int32.Parse(Regex.Match(command, @"\d+").Value);
                        cycle = true;

                        Console.Write(">");
                    }
                    else if (command.Contains("end-repeat"))
                    {

                        for (int i = 0; i < cycles; i++)
                        {
                            List<string> finalCommand = new List<string> { };
                            foreach (string s in repeatInstruction)
                                finalCommand.Add(s.Replace("$i", "" + i));

                            foreach (string s in finalCommand)
                            {
                                client.ParseCommand(s);
                            }
                        }

                        cycle = false;

                    }
                    else
                    {
                        if (cycle) {
                            repeatInstruction.Add(command); 
                        }
                        else
                        {
                            run = client.ParseCommand(command);
                        }
                    }

                } while (run);

                Console.ReadKey();
                server.ShutdownAsync().Wait();
                Environment.Exit(0);

            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            } 
        }
    }
}
