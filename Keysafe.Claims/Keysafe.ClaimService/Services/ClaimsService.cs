using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Keysafe.ClaimService
{
    public class ClaimsService : Claims.ClaimsBase
    {
        private readonly ILogger<ClaimsService> m_logger;
        public ClaimsService(ILogger<ClaimsService> logger)
        {
            m_logger = logger;
        }

        public override Task<VerifiableClaimsCreateReply> CreateVerifiableClaims(VerifiableClaimsCreateRequest request, ServerCallContext context)
        {
            return Task.FromResult(new VerifiableClaimsCreateReply
            {
                Success = false,
                Message = "Not implemented.",
                Attesthash = "N/A"
            });
        }
    }
}
