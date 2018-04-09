using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancingV2;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;
using AWSHelper.Extensions;


namespace AWSHelper.Route53
{
    public partial class Route53Helper
    {
        public async Task<IEnumerable<Amazon.Route53.Model.ResourceRecordSet>> ListResourceRecordSetsAsync(string zoneId)
        {
            string token = null;
            var list = new List<Amazon.Route53.Model.ResourceRecordSet>();
            Amazon.Route53.Model.ListResourceRecordSetsResponse response;
            while ((response = await _client.ListResourceRecordSetsAsync(
                new Amazon.Route53.Model.ListResourceRecordSetsRequest()
                {
                    StartRecordIdentifier = token,
                    HostedZoneId = zoneId

                }))?.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response?.ResourceRecordSets == null || response.ResourceRecordSets.Count <= 0)
                    break;

                list.AddRange(response.ResourceRecordSets);

                token = response.NextRecordIdentifier;
                if (token == null)
                    break;
            }

            response.EnsureSuccess();
            return list;
        }

    }
}
