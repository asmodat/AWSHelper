using System;
using AWSWrapper.ELB;
using AWSWrapper.Route53;
using AWSWrapper.ECS;
using AWSWrapper.CloudWatch;
using AsmodatStandard.IO;
using AsmodatStandard.Extensions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using AWSWrapper.KMS;
using AWSWrapper.IAM;
using AWSHelper.Extensions;

namespace AWSHelper.Fargate
{
    public static partial class FargateResourceHelperV2
    {
        public static async Task<List<Exception>> Destroy(
            FargateResourceV2 resource, 
            ELBHelper elb, Route53Helper e53, ECSHelper ecs, CloudWatchHelper cw, KMSHelper kms, IAMHelper iam, 
            bool throwOnFailure,
            bool catchDisable)
        {
            var errList = new List<Exception>();
            int maxRepeats = throwOnFailure ? 1 : 3;
            int delay_ms = throwOnFailure ? 500 : 10000;

            Console.WriteLine($"Destroying Role '{resource.RoleName}'...");
            (await iam.DeleteRoleAsync(resource.RoleName, detachPolicies: true)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms).CatchExceptionAsync()).PrintResult();

            Console.WriteLine($"Destroying Policy '{resource.PolicyNameAccessS3}'...");
            errList.Add(iam.DeletePolicyByNameAsync(resource.PolicyNameAccessS3, throwIfNotFound: false)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());

            Console.WriteLine($"Destroying Default Grant '{resource.StorageGrantDefaultS3}' for key '{resource.StorageKeyDefaultS3}'...");
            errList.Add(kms.RemoveGrantsByName(keyName: resource.StorageKeyDefaultS3, grantName: resource.StorageGrantDefaultS3, throwIfNotFound: false)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());

            Console.WriteLine($"Destroying Internal Grant '{resource.StorageGrantInternalS3}' for key '{resource.StorageKeyInternalS3}'...");
            errList.Add(kms.RemoveGrantsByName(keyName: resource.StorageKeyInternalS3, grantName: resource.StorageGrantInternalS3, throwIfNotFound: false)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());

            Console.WriteLine($"Destroying Application Load Balancer '{resource.LoadBalancerName}'...");
            errList.Add(elb.DestroyLoadBalancer(loadBalancerName: resource.LoadBalancerName, throwIfNotFound: false)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());

            if (resource.IsPublic && !resource.ZonePublic.IsNullOrWhitespace())
            {
                Console.WriteLine($"Destroying Route53 DNS Record: '{resource.DNSCName}' of '{resource.ZonePublic}' zone...");
                errList.Add(e53.DestroyCNameRecord(resource.ZonePublic, resource.DNSCName, throwIfNotFound: false)
                    .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                    .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());
            }

            if (!resource.ZonePrivate.IsNullOrWhitespace())
            {
                Console.WriteLine($"Destroying Route53 DNS Record: '{resource.DNSCName}' of '{resource.ZonePrivate}' zone...");
                errList.Add(e53.DestroyCNameRecord(resource.ZonePrivate, resource.DNSCName, throwIfNotFound: false)
                    .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                    .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());
            }

            Console.WriteLine($"Destroying Log Group '{resource.LogGroupName}'...");
            errList.Add(cw.DeleteLogGroupAsync(resource.LogGroupName, throwIfNotFound: false)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());

            Console.WriteLine($"Destroying Task Definitions of Family'{resource.TaskFamily}'...");
            errList.Add(ecs.DestroyTaskDefinitions(familyPrefix: resource.TaskFamily)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());

            Console.WriteLine($"Destroying Service '{resource.ServiceName}'...");
            errList.Add(ecs.DestroyService(cluster: resource.ClusterName, serviceName: resource.ServiceName, throwIfNotFound: false)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());

            Console.WriteLine($"Destroying Cluster '{resource.ClusterName}'...");
            errList.Add(ecs.DeleteClusterAsync(name: resource.ClusterName, throwIfNotFound: false)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());

            Console.WriteLine($"Destroying Metric Alarm '{resource.ELBHealthyMetricAlarmName}'...");
            errList.Add(cw.DeleteMetricAlarmAsync(resource.ELBHealthyMetricAlarmName, throwIfNotFound: false)
                .TryCatchRetryAsync(maxRepeats: maxRepeats, delay: delay_ms)
                .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult());

            if (throwOnFailure && errList.Any(x => x != null))
                throw new AggregateException("Failed Fargate Resource Destruction", errList.ToArray());

            return errList;
        }
    }
}
