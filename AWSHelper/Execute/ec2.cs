using System;
using AWSWrapper.EC2;
using AWSWrapper.IAM;
using AsmodatStandard.Extensions;
using System.Linq;
using AsmodatStandard.IO;
using Amazon.EC2;
using Amazon.SecurityToken.Model;
using AsmodatStandard.Extensions.Collections;
using static AWSWrapper.EC2.EC2Helper;
using Amazon.EC2.Model;
using AsmodatStandard.Extensions.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AWSHelper
{
    public partial class Program
    {
        private static async Task executeEC2(string[] args, Credentials credentials)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var ec2 = new EC2Helper();
            var iam = new IAMHelper(credentials);
            switch (args[1])
            {
                case "create-instance":
                    {
                        var imageId = nArgs["image"];
                        var keyName = nArgs["key"];
                        var instanceType = nArgs["instance-type"].ToEnum<InstanceModel>().ToInstanceType();
                        var securityGroupId = nArgs["security-group"];
                        var subnet = nArgs["subnet"];
                        var role = nArgs["role"];
                        var shutdownTermination = nArgs.GetValueOrDefault("on-shutdown-termination").ToBoolOrDefault(false);
                        var publicIp = nArgs.GetValueOrDefault("public-ip").ToBoolOrDefault(true);
                        var name = nArgs["name"];
                        var autoKill = nArgs.GetValueOrDefault("auto-kill").ToIntOrDefault(100 * 365 * 24 * 60);

                        var tt = DateTime.UtcNow.AddMinutes(autoKill);
                        var tt2 = tt.AddMinutes(15);
                        var tags = new System.Collections.Generic.Dictionary<string, string>()
                            {
                                { "Name", name },
                                { "Auto Kill", $"{tt.Minute}-{tt2.Minute} {tt.Hour}-{tt2.Hour} {tt.Day}-{tt2.Day} {tt.Month}-{tt2.Month} * {tt.Year}-{tt2.Year}" },
                            };

                        var cname = nArgs.GetValueOrDefault("cname");
                        var zones = nArgs.GetValueOrDefault("zones")?.Split(',')?.ToArray();

                        if (cname != null && zones != null)
                        {
                            for (int i = 0; i < zones.Length; i++)
                            {
                                var suffix = i > 0 ? $" {i + 1}" : "";
                                tags.Add($"Route53 Enable{suffix}", "true");
                                tags.Add($"Route53 Name{suffix}", cname);
                                tags.Add($"Route53 Zone{suffix}", zones[i]);
                            }
                        }

                        string instanceId;
                        var ebsOptymalized = nArgs.GetValueOrDefault("ebs-optymalized").ToBoolOrDefault();

                        if (nArgs.Any(x => x.Key.IsWildcardMatch("ebs-root-")))
                        {
                            Console.WriteLine("Advanced Instance Creation Initiated...");
                            var rootDeviceName = nArgs["ebs-root-dev-name"];
                            var rootSnapshotId = nArgs.GetValueOrDefault("ebs-root-snapshot-id");
                            var rootVolumeSize = nArgs["ebs-root-volume-size"].ToInt32();
                            var rootIOPS = nArgs["ebs-root-iops"].ToIntOrDefault(0);
                            var rootVolumeType = nArgs["ebs-root-volume-type"];

                            instanceId = ec2.CreateInstanceAsync(
                            imageId: imageId,
                            instanceType: instanceType,
                            keyName: keyName,
                            securityGroupIDs: new string[] { securityGroupId },
                            subnetId: subnet,
                            roleName: role,
                            shutdownBehavior: shutdownTermination ? ShutdownBehavior.Terminate : ShutdownBehavior.Stop,
                            associatePublicIpAddress: publicIp,
                            ebsOptymalized: ebsOptymalized,
                            rootDeviceName: rootDeviceName,
                            rootSnapshotId: rootSnapshotId,
                            rootVolumeSize: rootVolumeSize,
                            rootIOPS: rootIOPS,
                            rootVolumeType: rootVolumeType,
                            tags: tags
                            ).Result.Reservation.Instances.Single().InstanceId;
                        }
                        else
                        {
                            Console.WriteLine("Basic Instance Creation Initiated...");
                            instanceId = ec2.CreateInstanceAsync(
                            imageId: imageId,
                            instanceType: instanceType,
                            keyName: keyName,
                            securityGroupId: securityGroupId,
                            subnetId: subnet,
                            roleName: role,
                            shutdownBehavior: shutdownTermination ? ShutdownBehavior.Terminate : ShutdownBehavior.Stop,
                            associatePublicIpAddress: publicIp,
                            ebsOptymalized: ebsOptymalized,
                            tags: tags).Result.Reservation.Instances.Single().InstanceId;
                        }

                        if (nArgs.GetValueOrDefault("await-start").ToBoolOrDefault(false))
                        {
                            var timeout_ms = nArgs.GetValueOrDefault("await-start-timeout").ToIntOrDefault(5 * 60 * 1000);

                            Console.WriteLine($"Awaiting up to {timeout_ms} [ms] for instance '{instanceId}' to start...");

                            ec2.AwaitInstanceStateCode(instanceId,
                                EC2Helper.InstanceStateCode.running, timeout_ms: timeout_ms).Wait();
                        }

                        if (nArgs.GetValueOrDefault("await-system-start").ToBoolOrDefault(false))
                        {
                            var timeout_ms = nArgs.GetValueOrDefault("await-system-start-timeout").ToIntOrDefault(5 * 60 * 1000);

                            Console.WriteLine($"Awaiting up to {timeout_ms} [ms] for instance '{instanceId}' OS to start...");

                            ec2.AwaitInstanceStatus(instanceId,
                                EC2Helper.InstanceSummaryStatus.Ok, timeout_ms: timeout_ms).Wait();
                        }

                        Console.WriteLine($"SUCCESS, Instance '{instanceId}' was created.");
                    }
                    ; break;
                case "terminate-instance":
                    {
                        var name = nArgs.GetValueOrDefault("name");
                        Instance[] instances = null;
                        if (!name.IsNullOrEmpty())
                        {
                            instances = ec2.ListInstancesByName(name).Result;
                            Console.WriteLine($"Found {instances?.Length ?? 0} instances with name: '{name}'.");
                        }
                        else
                            throw new Exception("Not Supported Arguments");

                        instances.ParallelForEach(i =>
                        {
                            void TryRemoveTags()
                            {
                                if (!nArgs.GetValueOrDefault("try-delete-tags").ToBoolOrDefault(false))
                                    return;

                                var err = ec2.DeleteAllInstanceTags(i.InstanceId).CatchExceptionAsync().Result;

                                if (err == null)
                                    Console.WriteLine("Removed instance tags.");
                                else
                                    Console.WriteLine($"Failed to remove instance tags, Error: {err.JsonSerializeAsPrettyException()}");
                            }

                            if (i.State.Code == (int)InstanceStateCode.terminating ||
                            i.State.Code == (int)InstanceStateCode.terminated)
                            {
                                Console.WriteLine($"Instance {i.InstanceId} is already terminating or terminated.");
                                TryRemoveTags();
                                return;
                            }

                            Console.WriteLine($"Terminating {i.InstanceId}...");
                            var result = ec2.TerminateInstance(i.InstanceId).Result;
                            Console.WriteLine($"Instance {i.InstanceId} state changed {result.PreviousState.Name} -> {result.CurrentState.Name}");
                            TryRemoveTags();
                        });

                        Console.WriteLine($"SUCCESS, All Instances Are Terminated.");
                    }
                    ; break;
                case "describe-instance":
                    {
                        var name = nArgs.GetValueOrDefault("name");
                        Instance instance = null;
                        if (name != null)
                        {
                            instance = ec2.ListInstancesByName(name: name).Result
                                .SingleOrDefault(x => (x.State.Name != InstanceStateName.Terminated) && (x.State.Name != InstanceStateName.ShuttingDown));

                            if (instance == null)
                                throw new Exception($"No non terminated instance with name '{name}' was found.");

                            var property = nArgs.GetValueOrDefault("property");
                            var output = nArgs.GetValueOrDefault("output");

                            if (!property.IsNullOrEmpty())
                            {
                                
                                var value = instance.GetType().GetProperty(property).GetValue(instance, null);
                                var strValue = TypeEx.IsSimple(value.GetType().GetTypeInfo()) ? 
                                    value.ToString() : 
                                    value.JsonSerialize(Newtonsoft.Json.Formatting.Indented);

                                Console.WriteLine($"Instance '{instance.InstanceId}' Property '{property}', Value: '{strValue}'.");

                                if (!output.IsNullOrEmpty())
                                {
                                    Console.WriteLine($"Saving Property Value into output file: '{output}'...");
                                    output.ToFileInfo().WriteAllText(strValue);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Instance '{instance.InstanceId}' properties: {instance.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");
                                if (!output.IsNullOrEmpty())
                                {
                                    Console.WriteLine($"Saving Properties into output file: '{output}'...");
                                    output.ToFileInfo().WriteAllText(instance.JsonSerialize(Newtonsoft.Json.Formatting.Indented));
                                }
                            }
                        }
                        else
                            throw new Exception("Only describe property by name option is available.");

                        Console.WriteLine($"SUCCESS, instance '{instance.InstanceId}' properties were found.");
                    }
                    ; break;
                case "help":
                case "--help":
                case "-help":
                case "-h":
                case "h":
                    HelpPrinter($"{args[0]}", "Amazon Elastic Compute Cloud",
                    ("create-instance", "Accepts params: "),
                    ("terminate-instance", "Accepts params: name"));
                    break;
                default:
                    {
                        Console.WriteLine($"Try '{args[0]} help' to find out list of available commands.");
                        throw new Exception($"Unknown EC2 command: '{args[0]} {args[1]}'");
                    }
            }
        }
    }
}
