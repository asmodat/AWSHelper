using System;
using AWSWrapper.ELB;
using AWSWrapper.Route53;
using AWSWrapper.ECS;
using AWSWrapper.CloudWatch;
using AsmodatStandard.IO;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Threading;
using AsmodatStandard.Extensions.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Amazon.SecurityToken.Model;
using AWSWrapper.KMS;
using AWSWrapper.IAM;
using Newtonsoft.Json;
using System.Threading;
using AWSHelper.Extensions;
using AWSHelper.Fargate;
using System.Net;
using System.Diagnostics;
using Amazon.Route53.Model;
using Amazon.CloudWatch;
using Amazon.Route53;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeFargate(string[] args, Credentials credentials)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var elb = new ELBHelper();
            var r53 = new Route53Helper();
            var ecs = new ECSHelper();
            var cw = new CloudWatchHelper();
            var kms = new KMSHelper(credentials);
            var iam = new IAMHelper(credentials);

            switch (args[1])
            {
                case "create-resources":
                    {
                        bool catchDisable = nArgs.GetValueOrDefault("catch-disable", "false").ToBool();
                        int resourceCreateTimeout = nArgs["resource-create-timeout"].ToInt32();
                        int recordDnsUpdateTimeout = nArgs.GetValueOrDefault("record-dns-update-timeout").ToIntOrDefault(5 * 60 * 1000);

                        var resource = new FargateResourceV1();
                        resource.InitializeCreationParameters(nArgs);

                        string prefix_new = "a-";
                        string prefix_old = "b-";
                        bool setRoutes = true;

                        Console.WriteLine("Determining Temporary Resource Naming Conventions...");
                        var record = r53.GetCNameRecordSet(resource.IsPublic ? resource.ZonePublic : resource.ZonePrivate, resource.DNSCName,
                            failover: "PRIMARY",
                            throwIfNotFound: false).Result;

                        if (record?.ResourceRecords.IsNullOrEmpty() == false)
                        {
                            var a_alb = elb.GetLoadBalancersByName(loadBalancerName: $"a-{resource.LoadBalancerName}", throwIfNotFound: false).Result.SingleOrDefault();
                            var b_alb = elb.GetLoadBalancersByName(loadBalancerName: $"b-{resource.LoadBalancerName}", throwIfNotFound: false).Result.SingleOrDefault();

                            if (a_alb != null && record.ResourceRecords.Any(r => r.Value == a_alb.DNSName))
                            {
                                prefix_new = "b-";
                                prefix_old = "a-";
                                setRoutes = false;
                            }
                            else if (b_alb != null && record.ResourceRecords.Any(r => r.Value == b_alb.DNSName))
                            {
                                prefix_new = "a-";
                                prefix_old = "b-";
                                setRoutes = false;
                            }
                            else
                                Console.WriteLine("WARNING!!! Record was present, but could NOT find any associated loadbalancers.");
                        }

                        var resourceNew = resource.DeepCopy();
                        resourceNew.SetName($"{prefix_new}{resource.Name}");
                        resourceNew.SetDNSCName($"{prefix_new}{resource.DNSCName}");

                        var resourceOld = resource.DeepCopy();
                        resourceOld.SetName($"{prefix_old}{resource.Name}");
                        resourceOld.SetDNSCName($"{prefix_old}{resource.DNSCName}");

                        Console.WriteLine("Destroying Temporary Resources...");
                        FargateResourceHelperV1.Destroy(resourceNew, elb, r53, ecs, cw, kms, iam, throwOnFailure: true, catchDisable: catchDisable).Await();

                        try
                        {
                            Console.WriteLine("Creating New Resources...");
                            FargateResourceHelperV1.Create(resourceNew, elb, r53, ecs, cw, kms, iam);

                            Console.WriteLine($"Awaiting up to {resourceCreateTimeout} [s] for Tasks Desired Status...");
                            ecs.WaitForServiceToStart(resourceNew.ClusterName, resourceNew.ServiceName, resourceCreateTimeout).Await();
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"Failed New Resource Deployment with exception: {ex.JsonSerializeAsPrettyException(Formatting.Indented)}");

                            Console.WriteLine("Destroying New Resources...");
                            FargateResourceHelperV1.Destroy(resourceNew, elb, r53, ecs, cw, kms, iam, throwOnFailure: true, catchDisable: catchDisable).Await();

                            throw new Exception("New Resource Deployment Failure", ex);
                        }
                        
                        if (setRoutes ||
                            record?.HealthCheckId == null ||
                            record.HealthCheckId != r53.GetHealthCheckAsync(resource.HealthCheckName, throwIfNotFound: false).Result?.Id)
                        {
                            Console.WriteLine("DNS Route Initialization...");
                            FargateResourceHelperV1.SetRoutes(resource, resourceNew, elb, r53, cw);
                        }
                        else
                        {
                            Console.WriteLine("DNS Route Swap...");
                            FargateResourceHelperV1.SwapRoutes(resource, resourceNew, elb, r53, cw);
                        }

                        Console.WriteLine("Destroying Old Resources...");
                        FargateResourceHelperV1.Destroy(resourceOld, elb, r53, ecs, cw, kms, iam, throwOnFailure: true, catchDisable: catchDisable).Await();
                    }
                    ; break;
                case "destroy-resources":
                    {
                        bool catchDisable = nArgs.GetValueOrDefault("catch-disable", "false").ToBool();

                        var resource = new FargateResourceV1();
                        resource.InitializeTerminationParameters(nArgs);

                        var resourceA = resource.DeepCopy();
                        resourceA.SetName($"a-{resource.Name}");
                        resourceA.SetDNSCName($"a-{resource.DNSCName}");

                        var resourceB = resource.DeepCopy();
                        resourceB.SetName($"b-{resource.Name}");
                        resourceB.SetDNSCName($"b-{resource.DNSCName}");

                        var t0 = FargateResourceHelperV1.Destroy(resource, elb, r53, ecs, cw, kms, iam, throwOnFailure: true, catchDisable: catchDisable);
                        var t1 = FargateResourceHelperV1.Destroy(resourceA, elb, r53, ecs, cw, kms, iam, throwOnFailure: true, catchDisable: catchDisable);
                        var t2 = FargateResourceHelperV1.Destroy(resourceB, elb, r53, ecs, cw, kms, iam, throwOnFailure: true, catchDisable: catchDisable);

                        var result = Task.WhenAll(t0, t1, t2).Result;

                        Console.WriteLine($"Destroying Health Check'{resource.ELBHealthyMetricAlarmName}'...");
                        r53.DeleteHealthCheckByNameAsync(resource.HealthCheckName, throwIfNotFound: false)
                            .CatchExceptionAsync(catchDisable: catchDisable).Result.PrintResult();
                    }
                    break;
                case "help":
                case "--help":
                case "-help":
                case "-h":
                case "h":
                    HelpPrinter($"{args[0]}", "Amazon Fargate",
                    ("create-service", "Accepts params:"));
                    break;
                default:
                    {
                        Console.WriteLine($"Try '{args[0]} help' to find out list of available commands.");
                        throw new Exception($"Unknown Fargate command: '{args[0]} {args[1]}'");
                    }
            }
        }
    }
}