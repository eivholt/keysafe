using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Keysafe.ClaimService.Services.Message;
using Microsoft.Extensions.Logging;
using RestSharp;
using Tangle.Net.Cryptography;
using Tangle.Net.Entity;
using Tangle.Net.Mam.Entity;
using Tangle.Net.Mam.Merkle;
using Tangle.Net.Mam.Services;
using Tangle.Net.Repository;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        public override async Task<VerifiableClaimReply> GetVerifiableClaims(VerifiableClaimRequest request, ServerCallContext context)
        {
            m_logger.LogInformation("GetVerifiableClaim start");
            try 
            {
                // Connect to a node on one of the main networks.
                var iotaRepositoryUrl = Environment.GetEnvironmentVariable("IOTA_REPOSITORY");
                m_logger.LogInformation($"Using IOTA_REPOSITORY={iotaRepositoryUrl}");
                var repository = new RestIotaRepository(new RestClient(iotaRepositoryUrl));
                var factory = new MamChannelSubscriptionFactory(repository, CurlMamParser.Default, CurlMask.Default);
                var channelSubscription = factory.Create(new Hash(request.ChannelRoot), Mode.Restricted, request.SideKey);
                
                var publishedMessages = await channelSubscription.FetchAsync();
                return CreateVerifiableClaimReplyFromMessage(publishedMessages);
            }
            catch (Exception ex)
            {
                m_logger.LogError("CreateVerifiableClaims", ex);

                throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
            }
            finally
            {
                m_logger.LogInformation("GetVerifiableClaim end");
            }
        }

        private VerifiableClaimReply CreateVerifiableClaimReplyFromMessage(List<UnmaskedAuthenticatedMessage> publishedMessages)
        {
            var claims = new VerifiableClaimReply();
            foreach (var message in publishedMessages)
            {
                var claim = new VerifiableClaimReplyItem
                {
                    AttestHash = message.Message.Value,
                    NextRoot = message.NextRoot.Value,
                    ChannelRoot = message.Root.Value
                };

                claims.Claims.Add(claim);
            }

            return claims;
        }

        public override async Task<VerifiableClaimsCreateReply> CreateVerifiableClaims(VerifiableClaimsCreateRequest request, ServerCallContext context)
        {
            m_logger.LogInformation("CreateVerifiableClaims start");
            try
            {
                // Connect to a node on one of the main networks.
                var iotaRepositoryUrl = Environment.GetEnvironmentVariable("IOTA_REPOSITORY");
                m_logger.LogInformation($"Using IOTA_REPOSITORY={iotaRepositoryUrl}");
                var repository = new RestIotaRepository(new RestClient(iotaRepositoryUrl));
                var factory = new MamChannelFactory(CurlMamFactory.Default, CurlMerkleTreeFactory.Default, repository);
                var channel = factory.Create(Mode.Restricted, s_sampleLock1Seed, SecurityLevel.Medium, s_sampleLock1SideKey);

                var attestationJson = CreateAttestationFromRequest(request);
                m_logger.LogInformation($"Remove before production! Attestation JSON={attestationJson}");
                var message = channel.CreateMessage(TryteString.FromAsciiString(attestationJson));
            
                await channel.PublishAsync(message, minWeightMagnitude: 10);
                m_logger.LogInformation("Remove before production! CreateVerifiableClaims", message );

                return CreateVerifiableClaimsCreateReplyFromMessage(message);
            }
            catch (Exception ex)
            {
                m_logger.LogError("CreateVerifiableClaims", ex);

                throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
            }
            finally
            {
                m_logger.LogInformation("CreateVerifiableClaims end");
            }
        }

        private VerifiableClaimsCreateReply CreateVerifiableClaimsCreateReplyFromMessage(MaskedAuthenticatedMessage message)
        {
            var reply = new VerifiableClaimsCreateReply
            {
                Success = message.Payload.IsConfirmed,
                Message = "Attestation performed",
                AttestHash = message.Payload.Hash.Value,
                ChannelRoot = message.Root.Value
            };

            return reply;
        }

        private string CreateAttestationFromRequest(VerifiableClaimsCreateRequest request)
        {
            var attestationMessage = new AttestationMessage
            {
                AttestUuid = request.AttestUuid,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                User = new UserMessage 
                { 
                    Ssid = request.User.Ssid,
                    Name = request.User.Name,
                    Email = request.User.Email
                },
                Organization = new OrganizationMessage 
                { 
                    Name = request.Organization.Name,
                    Location  = request.Organization.Location,
                    PlaceOfWork = request.Organization.PlaceOfWork
                },
                Lock = new LockMessage 
                { 
                    Id = request.Lock.Id,
                    LocationAddress = request.Lock.LocationAddress
                }
            };

            return JsonSerializer.Serialize(attestationMessage);
        }
    }
}
