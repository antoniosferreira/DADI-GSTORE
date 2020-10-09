using System;
using System.Threading.Tasks;
using Grpc.Core;
using GSTORE_Server.PartitionModule;
using GSTORE_Server.PartitionModule.Exceptions;


namespace GSTORE_Server
{
    public class GStoreServerService : GStoreServerServices.GStoreServerServicesBase
    {
        private static Partition Partition = new Partition("s01");


        public GStoreServerService() { }
       

        // READ OPERATION
        public override Task<ReadReply> Read(ReadRequest request, ServerCallContext context)
        {
            return Task.FromResult(EncapsulateReadReply(request));
        }

        private ReadReply EncapsulateReadReply(ReadRequest request)
        {
            Tuple<bool, string> content = PerformRead(request.Key);
            return new ReadReply
            {
                Success = content.Item1,
                Value = content.Item2
            };
        }


        private Tuple<bool, string> PerformRead(string key)
        {
            try
            {
                string value = Partition.ReadValue(key);
                return Tuple.Create(true, value);


            } catch (InexistentKeyException)
            {
                string value = "The key " + key + " doesn't exist;";
                return Tuple.Create(false, value);
            }
            
        }


        // WRITE OPERATION
        public override Task<WriteReply> Write(WriteRequest request, ServerCallContext context)
        {

            Partition.WriteValue(request.Key, request.Value);


            return Task.FromResult(new WriteReply
            {
                Success = true,
                Message = request.Value
            });
        }

        private WriteReply EncapsulateWriteReply(WriteRequest request)
        {
            Tuple<bool, string> content = PerformWrite(request.Key, request.Value);
            return new WriteReply
            {
                Success = content.Item1,
                Message = content.Item2
            };
        }

        private Tuple<bool, string> PerformWrite(string key, string value)
        {
            string writtenValue = Partition.WriteValue(key, value);
            if (writtenValue.Equals(value))
                return Tuple.Create(true, value);
            else
                return Tuple.Create(false, "Something went wrong writing " + value);
        }
    }
}
