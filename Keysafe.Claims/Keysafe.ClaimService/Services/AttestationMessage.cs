using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Keysafe.ClaimService.Services
{
    public class AttestationMessage
    {
        public string AttestUuid { get; set; }
        public Google.Protobuf.WellKnownTypes.Timestamp ValidFrom { get; set; }
        public Google.Protobuf.WellKnownTypes.Timestamp ValidTo { get; set; }
        User user = 4;
        Organization organization = 5;
        Lock lock = 6;
    }
}
