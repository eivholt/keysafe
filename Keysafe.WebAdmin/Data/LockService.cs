using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using GrpcGreeter;

namespace Keysafe.WebAdmin.Data
{
    public class LockService
    {
        public async Task<HelloReply> GetLocksAsync(string user)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Greeter.GreeterClient(channel);
            return await client.SayHelloAsync(
                              new HelloRequest { Name = user });
        }
    }
}
