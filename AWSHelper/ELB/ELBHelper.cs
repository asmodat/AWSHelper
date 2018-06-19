using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancingV2;
using AsmodatStandard.Threading;
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

        public Task DeleteListenersAsync(IEnumerable<string> arns) => arns.ForEachAsync(
            arn => _clientV2.DeleteListenerAsync(
                    new Amazon.ElasticLoadBalancingV2.Model.DeleteListenerRequest() { ListenerArn = arn }),
                    _maxDegreeOfParalelism
            ).EnsureSuccess();

        public Task DeleteTargetGroupsAsync(IEnumerable<string> arns) => arns.ForEachAsync(arn =>
                _clientV2.DeleteTargetGroupAsync(
                    new Amazon.ElasticLoadBalancingV2.Model.DeleteTargetGroupRequest() { TargetGroupArn = arn }),
                    _maxDegreeOfParalelism
            ).EnsureSuccess();

        public Task DeleteLoadBalancersAsync(IEnumerable<string> arns) => arns.ForEachAsync(
            arn => _clientV2.DeleteLoadBalancerAsync(
                    new Amazon.ElasticLoadBalancingV2.Model.DeleteLoadBalancerRequest() { LoadBalancerArn = arn }),
                    _maxDegreeOfParalelism
            ).EnsureSuccess();
    }
}
