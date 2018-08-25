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

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeEC2(string[] args, Credentials credentials)
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
                                var suffix = i > 0 ? $" {i+1}" : "";
                                tags.Add($"Route53 Enable{suffix}", "true");
                                tags.Add($"Route53 Name{suffix}", cname);
                                tags.Add($"Route53 Zone{suffix}", zones[i]);
                            }
                        }

                        var instanceId = ec2.CreateInstanceAsync(
                            imageId: imageId,
                            instanceType: instanceType,
                            keyName: keyName,
                            securityGroupId: securityGroupId,
                            subnetId: subnet,
                            roleName: role,
                            shutdownBehavior: shutdownTermination ? ShutdownBehavior.Terminate : ShutdownBehavior.Stop,
                            associatePublicIpAddress: publicIp,
                            tags: tags).Result.Reservation.Instances.Single().InstanceId;

                        if (nArgs.GetValueOrDefault("await-start").ToBoolOrDefault(true))
                        {
                            var timeout_ms = nArgs.GetValueOrDefault("await-start-timeout").ToIntOrDefault(5 * 60 * 1000);

                            Console.WriteLine($"Awaiting up to {timeout_ms} [ms] for instance '{instanceId}' to start...");

                            ec2.AwaitInstanceStateCode(instanceId,
                                EC2Helper.InstanceStateCode.running, timeout_ms: timeout_ms).Wait();
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

                        instances.ParallelForEach(i => {
                            if (i.State.Code == (int)InstanceStateCode.terminating ||
                                i.State.Code == (int)InstanceStateCode.terminated)
                            {
                                Console.WriteLine($"Instance {i.InstanceId} is already terminating or terminated.");
                                return;
                            }

                            Console.WriteLine($"Terminating {i.InstanceId}...");
                            var result = ec2.TerminateInstance(i.InstanceId).Result;
                            Console.WriteLine($"Instance {i.InstanceId} state changed {result.PreviousState.Name} -> {result.CurrentState.Name}");
                        });

                        Console.WriteLine($"SUCCESS, All Instances Are Terminated.");
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
