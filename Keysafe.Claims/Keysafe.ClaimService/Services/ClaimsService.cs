using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using RestSharp;
using Tangle.Net.Cryptography;
using Tangle.Net.Entity;
using Tangle.Net.Mam.Entity;
using Tangle.Net.Mam.Merkle;
using Tangle.Net.Mam.Services;
using Tangle.Net.Repository;

namespace Keysafe.ClaimService
{
    public class ClaimsService : Claims.ClaimsBase
    {
        private readonly ILogger<ClaimsService> m_logger;
        private static readonly Seed s_sampleLock1Seed = new Seed("W9XOWBFMSZKXNLHZWDTPUQTW99OSPFHSLEGAQMR9FDSLYUPGCMZBGCEXCN9BBW9CBNHNQACFHYJEQOU99");
        private static readonly string s_sampleLock1SideKey = "E&AVejgjEk36k@-%";

        public ClaimsService(ILogger<ClaimsService> logger)
        {
            m_logger = logger;
        }

        public override async Task<VerifiableClaimsCreateReply> CreateVerifiableClaims(VerifiableClaimsCreateRequest request, ServerCallContext context)
        {
            // Connect to a node on one of the main networks.
            var repository = new RestIotaRepository(new RestClient("https://nodes.devnet.thetangle.org:443"));
            var factory = new MamChannelFactory(CurlMamFactory.Default, CurlMerkleTreeFactory.Default, repository);
            var channel = factory.Create(Mode.Restricted, s_sampleLock1Seed, SecurityLevel.Medium, s_sampleLock1SideKey);

            var attestation = CreateAttestationFromRequest(request);
            var message = channel.CreateMessage(TryteString.FromAsciiString(request.));

            try
            {
                await channel.PublishAsync(message);
                m_logger.LogInformation("CreateVerifiableClaims, remove before production!", message );

                return new VerifiableClaimsCreateReply
                {
                    Success = false,
                    Message = "Not implemented.",
                    AttestHash = "N/A"
                };
            }
            catch (Exception ex)
            {
                m_logger.LogError("CreateVerifiableClaims", ex);

                return new VerifiableClaimsCreateReply
                {
                    Success = false,
                    Message = ex.Message + ex.StackTrace,
                    AttestHash = "error"
                };
            }
        }

        private string CreateAttestationFromRequest(VerifiableClaimsCreateRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
