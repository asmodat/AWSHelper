using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Route53;
using AWSHelper.Extensions;

namespace AWSHelper.Route53
{
    public partial class Route53Helper
    {
        private readonly int _maxDegreeOfParalelism;
        private readonly AmazonRoute53Client _client;

        public Route53Helper(int maxDegreeOfParalelism = 8)
        {
            _maxDegreeOfParalelism = maxDegreeOfParalelism;
            _client = new AmazonRoute53Client();
        }

        public Task ChangeResourceRecordSetsAsync(
            string zoneId,
            Amazon.Route53.Model.ResourceRecordSet resourceRecordSet,
            Amazon.Route53.Model.Change change) => _client.ChangeResourceRecordSetsAsync(
                     new Amazon.Route53.Model.ChangeResourceRecordSetsRequest()
                     {
                         ChangeBatch = new Amazon.Route53.Model.ChangeBatch()
                         {
                             Changes = new List<Amazon.Route53.Model.Change>() {
                                 change
                             }
                         },
                         HostedZoneId = zoneId
                     }).EnsureSuccessAsync();

        public Task DeleteResourceRecordSetsAsync(string zoneId, Amazon.Route53.Model.ResourceRecordSet resourceRecordSet)
            => ChangeResourceRecordSetsAsync(zoneId, resourceRecordSet, new Amazon.Route53.Model.Change()
            {
                Action = new ChangeAction(ChangeAction.DELETE),
                ResourceRecordSet = resourceRecordSet
            });
    }
}
