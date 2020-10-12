using System.Threading.Tasks;
using Grpc.Core;
using System.IO;
using System;
using System.Diagnostics;

namespace PCS
{
    public class PCSService : PCSServices.PCSServicesBase
    {

        public PCSService() { }




        // INIT SERVER OPERATION
        public override Task<Void> InitServer(ServerRequest request, ServerCallContext context)
        {
            Process serverProcess = new Process();

            // Finds GSTORE-SERVER executable
            try
            {
                string executablePath = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\GSTORE-SERVER\\bin\\Debug\\netcoreapp3.1\\GSTORE-Server.exe";

                serverProcess.StartInfo.UseShellExecute = true;
                serverProcess.StartInfo.FileName = executablePath;
                serverProcess.StartInfo.Arguments =
                        string.Format("{0} {1} {2} {3}",
                        request.ServerID, request.ServerURL, request.MinDelay,
                        request.MaxDelay);

                serverProcess.StartInfo.CreateNoWindow = false;
                serverProcess.Start();
            } catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            

            return Task.FromResult(new Void { });
        }

        // INIT SERVER OPERATION
        public override Task<Void> InitClient(ClientRequest request, ServerCallContext context)
        {

            Process serverProcess = new Process();

            // Finds GSTORE-Client executable
            try
            {
                string executablePath = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + "\\GSTORE-Client\\bin\\Debug\\netcoreapp3.1\\GSTORE-Client.exe";

                serverProcess.StartInfo.UseShellExecute = true;
                serverProcess.StartInfo.FileName = executablePath;
                serverProcess.StartInfo.Arguments =
                        string.Format("{0} {1} {2}",
                        request.Username, request.ClientUrl, request.ScriptFile);

                serverProcess.StartInfo.CreateNoWindow = false;
                serverProcess.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }


            return Task.FromResult(new Void { });

        }

    }
}
