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

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeFargate(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var elb = new ELBHelper();
            var e53 = new Route53Helper();
            var ecs = new ECSHelper();
            var cw = new CloudWatchHelper();

            switch (args[1])
            {
                case "create-resources":
                    {
                        var name = nArgs["name"];
                        var cname = nArgs["cname"];
                        var image = nArgs["image"];
                        var zonePublic = nArgs.GetValueOrDefault("zone-public");
                        var zonePrivate = nArgs.GetValueOrDefault("zone-private");
                        var subnets = nArgs["subnets"].Split(',');
                        var securityGroups = nArgs["security-groups"].Split(',');
                        var role = nArgs["role"];
                        var cpu = nArgs.GetValueOrDefault("cpu").ToIntOrDefault(256);
                        var memory = nArgs.GetValueOrDefault("memory").ToIntOrDefault(512);
                        var desiredCount = nArgs.GetValueOrDefault("desired-count").ToIntOrDefault(1);
                        var @public = nArgs.GetValueOrDefault("public").ToBool();
                        var port = nArgs["port"].ToInt32();
                        var ports = nArgs["ports"].Split(',').Select(x => x.ToInt32());
                        var vpc = nArgs["vpc"];
                        var healthCheckPath = nArgs["health-check-path"];
                        var environment = nArgs["environment"].Split(',').Select(x =>
                        {
                            var split = x.SplitByFirst(':');
                            return new KeyValuePair<string, string>(split[0], split.Length == 1 ? "" : split[1]);
                        });

                        if (zonePrivate.IsNullOrWhitespace() || zonePrivate.IsNullOrWhitespace())
                            throw new Exception("Either 'zone-public' or 'zone-private' parameter must be specified.");

                        var cluster = $"{name}-ecs";
                        var albName = $"{name}-alb-{(@public ? "pub" : "prv")}";
                        var tgName = $"{name}-tg-{(@public ? "pub" : "prv")}";
                        var lgName = $"ecs-lg-{name}-{(@public ? "pub" : "prv")}";
                        var family = $"{name}-ecs-tsk-fam-{(@public ? "pub" : "prv")}";
                        var taskDefName = $"{name}-ecs-tsk-def-{(@public ? "pub" : "prv")}";
                        var serviceName = $"{name}-service-{(@public ? "public" : "private")}";

                        Console.WriteLine("Destroying Application Load Balancer...");
                        elb.DestroyLoadBalancer(loadBalancerName: albName, throwIfNotFound: false).Await();
                        Console.WriteLine("Crating Application Load Balancer...");
                        var loadBalancer = elb.CreateApplicationLoadBalancerAsync(albName, subnets, securityGroups, !@public).Result;
                        Console.WriteLine($"Finished Creatrion of Application Load Balancer: {loadBalancer.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");

                        Console.WriteLine("Creating Target Group...");
                        var targetGroup = elb.CreateHttpTargetGroupAsync(tgName, port, vpc, healthCheckPath).Result;
                        Console.WriteLine($"Finished Target Group Creation: {targetGroup.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");

                        Console.WriteLine("Creating Listener...");
                        var listener = elb.CreateHttpListenerAsync(loadBalancer.LoadBalancerArn, targetGroup.TargetGroupArn, port).Result;
                        Console.WriteLine($"Finished Listener Creation: {listener.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");

                        if (@public && !zonePublic.IsNullOrWhitespace())
                        {
                            Console.WriteLine("Creating Route53 DNS Record for the public zone...");
                            var zonePub = e53.GetHostedZoneAsync(zonePublic).Result;
                            e53.UpsertCNameRecordAsync(
                                zonePublic,
                                name: $"www.{cname}.{zonePub.HostedZone.Name.TrimEnd('.')}",
                                value: loadBalancer.DNSName,
                                ttl: 60).Await();
                        }

                        if (!zonePrivate.IsNullOrWhitespace())
                        {
                            Console.WriteLine("Creating Route53 DNS Record for the private zone...");
                            var zonePrv = e53.GetHostedZoneAsync(zonePrivate).Result;
                            e53.UpsertCNameRecordAsync(
                                zonePrivate,
                                name: $"www.{cname}.{zonePrv.HostedZone.Name.TrimEnd('.')}",
                                value: loadBalancer.DNSName,
                                ttl: 60).Await();
                        }

                        Console.WriteLine("Initializeing Cluster...");
                        var createClusterResponse = ecs.CreateClusterAsync(cluster).Result;
                        Console.WriteLine($"Finished Cluster Initialization: {createClusterResponse.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");

                        Console.WriteLine("Destroying Log Group...");
                        cw.DeleteLogGroupAsync(lgName, throwIfNotFound: false).Await();

                        Console.WriteLine("Creating Log Group...");
                        cw.CreateLogGroupAsync(lgName).Await();

                        Console.WriteLine("Destroying Task Definitions...");
                        ecs.DestroyTaskDefinitions(familyPrefix: family).Await();

                        Console.WriteLine("Creating Task Definitions...");
                        var taskDefinition = ecs.RegisterFargateTaskAsync(
                            executionRoleArn: role,
                            family: family,
                            cpu: cpu,
                            memory: memory,
                            name: taskDefName,
                            image: image,
                            envVariables: new Dictionary<string, string>(environment),
                            logGroup: lgName,
                            ports: ports).Result;
                        Console.WriteLine($"Finished Task Definition: {taskDefinition.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");

                        Console.WriteLine("Destroying Service...");
                        ecs.DestroyService(cluster: cluster, serviceName: serviceName, throwIfNotFound: false).Await();

                        Console.WriteLine("Creating Service...");
                        var service = ecs.CreateFargateServiceAsync(
                            name: serviceName,
                            taskDefinition: taskDefinition,
                            desiredCount: desiredCount,
                            cluster: cluster,
                            targetGroup: targetGroup,
                            assignPublicIP: @public,
                            securityGroups: securityGroups,
                            subnets: subnets
                            ).Result;

                        Console.WriteLine($"Finished Creating Service: {service.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");
                    }
                    ; break;
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
