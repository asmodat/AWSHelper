using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECS;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;

namespace AWSHelper.ELB
{
    public static class ELBHelperEx
    {
        public static async Task<IEnumerable<string>> ListListenersAsync(this ELBHelper elbh, string loadBalancerArn)
           => (await elbh.DescribeListenersAsync(loadBalancerArn)).Select(x => x.ListenerArn);

        public static async Task<IEnumerable<string>> ListTargetGroupsAsync(this ELBHelper elbh, string loadBalancerArn,
            IEnumerable<string> names = null, IEnumerable<string> targetGroupArns = null)
            => (await elbh.DescribeTargetGroupsAsync(loadBalancerArn, names, targetGroupArns)).Select(x => x.TargetGroupArn);

        public static async Task DestroyLoadBalancer(this ELBHelper elbh, string loadBalancerName)
        {
            var loadbalancers = await elbh.DescribeLoadBalancersAsync(new List<string>() { loadBalancerName });

            if (loadbalancers.Count() != 1)
                throw new Exception($"DestroyLoadBalancer, LoadBalancer '{loadBalancerName}' was not found.");

            var arn = loadbalancers.First().LoadBalancerArn;
            var listeners = await elbh.ListListenersAsync(arn);
            var targetGroups = await elbh.ListTargetGroupsAsync(arn);

            //kill listeners
            await elbh.DeleteListenersAsync(listeners);

            //kill target groups
            await elbh.DeleteTargetGroupsAsync(targetGroups);

            //kill loadbalancer
            await elbh.DeleteLoadBalancersAsync(new List<string>() { arn });
        }
    }
}
