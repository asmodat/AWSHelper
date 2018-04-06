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

namespace AWSHelper.ELB
{
    public partial class ELBHelper
    {
        private readonly int _maxDegreeOfParalelism;
        private readonly AmazonElasticLoadBalancingClient _client;
        private readonly AmazonElasticLoadBalancingV2Client _clientV2;

        public ELBHelper(int maxDegreeOfParalelism = 8)
        {
            _maxDegreeOfParalelism = maxDegreeOfParalelism;
            _client = new AmazonElasticLoadBalancingClient();
            _clientV2 = new AmazonElasticLoadBalancingV2Client();
        }

        public async Task DeleteListenersAsync(IEnumerable<string> arns)
        {
            var responses = await arns.ForEachAsync(arn =>
                _clientV2.DeleteListenerAsync(
                    new Amazon.ElasticLoadBalancingV2.Model.DeleteListenerRequest() { ListenerArn = arn }),
                    _maxDegreeOfParalelism
            );

            responses.EnsureSuccess();
        }

        public async Task DeleteTargetGroupsAsync(IEnumerable<string> arns)
        {
            var responses = await arns.ForEachAsync(arn =>
                _clientV2.DeleteTargetGroupAsync(
                    new Amazon.ElasticLoadBalancingV2.Model.DeleteTargetGroupRequest() { TargetGroupArn = arn}),
                    _maxDegreeOfParalelism
            );

            responses.EnsureSuccess();
        }

        public async Task DeleteLoadBalancersAsync(IEnumerable<string> arns)
        {
            var responses = await arns.ForEachAsync(arn =>
                _clientV2.DeleteLoadBalancerAsync(
                    new Amazon.ElasticLoadBalancingV2.Model.DeleteLoadBalancerRequest() { LoadBalancerArn = arn }),
                    _maxDegreeOfParalelism
            );

            responses.EnsureSuccess();
        }
    }
}
