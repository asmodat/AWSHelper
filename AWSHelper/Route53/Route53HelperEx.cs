using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECS;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;

namespace AWSHelper.Route53
{
    public static class Route53HelperEx
    {
        public static async Task<Amazon.Route53.Model.ResourceRecordSet> GetRecordSet(this Route53Helper r53h, string zoneId, string recordName, string recordType)
        {
            var set = await r53h.ListResourceRecordSetsAsync(zoneId);
            set = set?.Where(x => x.Name == recordName && x.Type == recordType);

            if (set?.Count() != 1)
                throw new Exception($"DestroyRecord Failed, RecordSet with Name: '{recordName}' and Type: '{recordType}' was not found, or more then one was found. [{set?.Count()}]");

            return set.First();
        }

        public static async Task DestroyRecord(this Route53Helper r53h, string zoneId, string recordName, string recordType)
            => await r53h.ChangeResourceRecordSetsAsync(zoneId, await r53h.GetRecordSet(zoneId, recordName, recordType));
    }
}
