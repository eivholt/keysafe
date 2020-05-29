using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Keysafe.ClaimService;
using Tangle.Net.Entity;

namespace Keysafe.WebAdmin.Data
{
    public class ClaimsServiceFacade
    {
        public async Task<VerifiableClaimReply> GetVerifiableClaims(VerifiableClaimRequest request)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Claims.ClaimsClient(channel);
            return await client.GetVerifiableClaimsAsync(request);
        }

        public async Task<PublishVerifiableClaimsReply> PublishVerifiableClaims(PublishVerifiableClaimsRequest request)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Claims.ClaimsClient(channel);
            return await client.PublishVerifiableClaimsAsync(request);
        }
    }
}
