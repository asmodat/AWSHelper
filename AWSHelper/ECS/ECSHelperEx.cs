using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECS;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;
using AsmodatStandard.Types;

namespace AWSHelper.ECS
{
    public static class ECSHelperEx
    {
        public static async Task<IEnumerable<ServiceInfo>> ListServicesAsync(this ECSHelper ecs)
        {
            var clusetrs = await ecs.ListClustersAsync();
            var result = await clusetrs.ForEachAsync(cluster => ListServicesAsync(ecs, cluster), 8);
            return result.Flatten();
        }

        public static async Task<IEnumerable<ServiceInfo>> ListServicesAsync(this ECSHelper ecs, string cluster)
        {
            var tFargateServices = ecs.ListServicesAsync(cluster, LaunchType.FARGATE);
            var tEC2Services = ecs.ListServicesAsync(cluster, LaunchType.EC2);
            await Task.WhenAll(tFargateServices, tEC2Services);
            return tFargateServices.Result.Select(x => new ServiceInfo(cluster: cluster, arn: x, launchType: LaunchType.FARGATE))
                .ConcatOrDefault(tEC2Services.Result.Select(x => new ServiceInfo(cluster: cluster, arn: x, launchType: LaunchType.EC2)));
        }

        public static Task UpdateServiceAsync(this ECSHelper ecs, int desiredCount, string cluster, params string[] arns)
            => ecs.UpdateServicesAsync(arns, desiredCount, cluster);

        public static Task DeleteServiceAsync(this ECSHelper ecs, string cluster, params string[] arns)
            => ecs.DeleteServicesAsync(arns, cluster: cluster);

        public static async Task DestroyService(this ECSHelper ecs, string cluster, string serviceName)
        {
            var services = await ((cluster.IsNullOrEmpty()) ? ecs.ListServicesAsync() : ecs.ListServicesAsync(cluster));

            services = services.Where(x => ((serviceName.StartsWith("arn:")) ? x.ARN == serviceName : x.ARN.EndsWith($":service/{serviceName}")));

            if (services?.Count() != 1)
                throw new Exception($"Could not find service '{serviceName}' for cluster: '{cluster}' or found more then one matching result (In such case use ARN insted of serviceName, or specify cluster) [{services?.Count()}].");

            var service = services.First();
            var tasks = await ecs.ListTasksAsync(service.Cluster, service.ARN);
            await ecs.UpdateServiceAsync(desiredCount: 0, arns: service.ARN, cluster: service.Cluster);
            await ecs.DeleteServiceAsync(cluster: service.Cluster, arns: service.ARN);
            await ecs.StopTasksAsync(arns: tasks, cluster: service.Cluster);
        }

        public static async Task DestroyTaskDefinitions(this ECSHelper ecs, string familyPrefix)
        {
            var tasks = await ecs.ListTaskDefinitionsAsync(familyPrefix);
            await ecs.DeregisterTaskDefinitionsAsync(tasks);
        }

        public static async Task WaitForServiceToStart(this ECSHelper ecs, string cluster, string serviceName, int timeout)
        {
            var services = await ((cluster.IsNullOrEmpty()) ? ecs.ListServicesAsync() : ecs.ListServicesAsync(cluster));

            services = services.Where(x => ((serviceName.StartsWith("arn:")) ? x.ARN == serviceName : x.ARN.EndsWith($":service/{serviceName}")));

            if (services?.Count() != 1)
                throw new Exception($"Could not find service '{serviceName}' for cluster: '{cluster}' or found more then one matching result (In such case use ARN insted of serviceName, or specify cluster) [{services?.Count()}].");

            var service = services.First();

            var tt = new TickTimeout(timeout, TickTime.Unit.s, Enabled: true);
            while (!tt.IsTriggered)
            {
                var serviceDescription = await ecs.DescribeServicesAsync(service.Cluster, new string[] { service.ARN });

                if (serviceDescription.IsNullOrEmpty())
                    throw new Exception($"Could not find or describe service: '{service.ARN}' for the cluster '{service.Cluster}'.");

                var result = serviceDescription.First();

                if (result.DesiredCount == result.RunningCount)
                    return; //desired count reached

                if (result.PendingCount != 0)
                    await Task.Delay(1000);
            }

            throw new Exception($"Timeout '{timeout}' [s], service: '{service.ARN}' could not reach its desired count in time.");
        }
    }
}
