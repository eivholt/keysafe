using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Keysafe.ClaimService.Services.Message
{
    public class AttestationMessage
    {
        public string SchemaVersion { get { return "v0.1"; } }
        public string AttestUuid { get; set; }
        public Google.Protobuf.WellKnownTypes.Timestamp ValidFrom { get; set; }
        public Google.Protobuf.WellKnownTypes.Timestamp ValidTo { get; set; }
        public UserMessage User { get; set; }
        public OrganizationMessage Organization { get; set; }
        public LockMessage Lock { get; set; }
    }

    public class UserMessage
    {
        public string Ssid { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class OrganizationMessage 
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string PlaceOfWork { get; set; }
    }

    public class LockMessage
    {
        public string Id { get; set; }
        public string LocationAddress { get; set; }
    }
}