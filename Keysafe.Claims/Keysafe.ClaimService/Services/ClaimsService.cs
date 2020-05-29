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

namespace Keysafe.ClaimService
{
    public class ClaimsService : Claims.ClaimsBase
    {
        private readonly ILogger<ClaimsService> m_logger;
        private static MamChannelFactory m_mamChannelFactory;
        private static MamChannelSubscriptionFactory m_mamChannelSubscriptionFactory;
        private static RestIotaRepository m_restIotaRepository;

        public ClaimsService(ILogger<ClaimsService> logger)
        {
            m_logger = logger;
            // Connect to a node on one of the main networks.
            var iotaRepositoryUrl = Environment.GetEnvironmentVariable("IOTA_REPOSITORY");
            m_logger.LogInformation($"Using IOTA_REPOSITORY={iotaRepositoryUrl}");
            m_restIotaRepository = new RestIotaRepository(new RestClient(iotaRepositoryUrl));
            m_mamChannelFactory = new MamChannelFactory(CurlMamFactory.Default, CurlMerkleTreeFactory.Default, m_restIotaRepository);
            m_mamChannelSubscriptionFactory = new MamChannelSubscriptionFactory(m_restIotaRepository, CurlMamParser.Default, CurlMask.Default);
        }

        public override async Task<VerifiableClaimReply> GetVerifiableClaims(VerifiableClaimRequest request, ServerCallContext context)
        {
            m_logger.LogInformation("GetVerifiableClaim start");
            try 
            {
                var channelSubscription = m_mamChannelSubscriptionFactory.Create(new Hash(request.ChannelRoot), Mode.Restricted, channelKey: new TryteString(request.SideKey).Value, keyIsTrytes: true);
                var publishedMessages = await channelSubscription.FetchAsync();
                m_logger.LogInformation("Remove before production! GetVerifiableClaims", publishedMessages.ToString());
                var reply = CreateVerifiableClaimReplyFromMessage(publishedMessages);
                m_logger.LogInformation("Remove before production! GetVerifiableClaims", reply.ToString());
                return reply;
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
                    ChannelRoot = message.Root.Value,
                    Message = message.Message.ToAsciiString()
                };

                claims.Claims.Add(claim);
            }

            return claims;
        }

        public override async Task<PublishVerifiableClaimsReply> PublishVerifiableClaims(PublishVerifiableClaimsRequest request, ServerCallContext context)
        {
            m_logger.LogInformation("PublishVerifiableClaims start");
            try
            {
                var channel = m_mamChannelFactory.Create(Mode.Restricted, new Seed(request.Seed), securityLevel: SecurityLevel.Medium, channelKey: request.SideKey, keyIsTrytes: true);
                var attestationJson = CreateAttestationFromRequest(request);
                m_logger.LogInformation($"Remove before production! PublishVerifiableClaims Attestation JSON={attestationJson}");
                var message = channel.CreateMessage(TryteString.FromUtf8String(attestationJson));
            
                await channel.PublishAsync(message, minWeightMagnitude: 10);
                m_logger.LogInformation($"Remove before production! PublishVerifiableClaims Message={JsonSerializer.Serialize(message)}");


                return CreatePublishVerifiableClaimsReplyFromMessage(message);
            }
            catch (Exception ex)
            {
                m_logger.LogError("CreateVerifiableClaims", ex);

                throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
            }
            finally
            {
                m_logger.LogInformation("PublishVerifiableClaims end");
            }
        }

        private PublishVerifiableClaimsReply CreatePublishVerifiableClaimsReplyFromMessage(MaskedAuthenticatedMessage message)
        {
            var reply = new PublishVerifiableClaimsReply
            {
                IsConfirmed = message.Payload.IsConfirmed,
                Message = "Attestation performed",
                AttestHash = message.Payload.Hash.Value,
                ChannelRoot = message.Root.Value,
                NextRoot = message.NextRoot.Value,
                Address = message.Address.Value
            };

            return reply;
        }

        private string CreateAttestationFromRequest(PublishVerifiableClaimsRequest request)
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
