using System;
using AWSWrapper.ELB;
using AsmodatStandard.IO;
using System.Linq;
using AWSWrapper.EC2;
using AsmodatStandard.Extensions.Collections;
using AsmodatStandard.Extensions;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeELB(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var elb = new ELBHelper();
            var ec2 = new EC2Helper();
            switch (args[1])
            {
                case "destroy-load-balancer":
                    elb.DestroyLoadBalancer(nArgs["name"], throwIfNotFound: true).Wait();
                    ; break;
                case "register-target-instance":
                    {
                        var tgName = nArgs["tg-name"];
                        var instanceName = nArgs["instance"];
                        var port = nArgs["port"].ToInt32();
                        var tg = elb.GetTargetGroupByNameAsync(
                            targetGroupName: tgName,
                            throwIfNotFound: true).Result;

                        var instance = ec2.ListInstancesByName(name: instanceName).Result.SingleOrDefault();

                        if(instance == null)
                            throw new Exception($"Could not find instance with name '{instanceName}' or found more then one.");

                        var result = elb.RegisterTargetAsync(tg, instance, port: port).Result;

                        Console.WriteLine($"Successfully Registered Instance '{instance.InstanceId}' into Target Group '{tg.TargetGroupArn}', response metadata: {result.ResponseMetadata?.JsonSerialize() ?? "undefined"}");
                    }
                    ; break;
                case "deregister-target-instance":
                    {
                        var tgName = nArgs["tg-name"];
                        var instanceName = nArgs["instance"];
                        var tg = elb.GetTargetGroupByNameAsync(
                            targetGroupName: tgName,
                            throwIfNotFound: true).Result;

                        var instance = ec2.ListInstancesByName(name: instanceName).Result.SingleOrDefault();

                        if (instance == null)
                            throw new Exception($"Could not find instance with name '{instanceName}' or found more then one.");

                        var result = elb.DeregisterTargetAsync(tg, instance).Result;

                        Console.WriteLine($"Successfully Deregistered Instance '{instance.InstanceId}' from Target Group '{tg.TargetGroupArn}', response metadata: {result.ResponseMetadata?.JsonSerialize() ?? "undefined"}");
                    }
                    ; break;
                case "help":
                case "--help":
                case "-help":
                case "-h":
                case "h":
                    HelpPrinter($"{args[0]}", "Amazon Elastic Load Balancer",
                    ("destroy-load-balancer", "Accepts params: name"),
                    ("register-target-instance", "Accepts params: tg-name, instance, port"),
                    ("deregister-target-instance", "Accepts params: tg-name, instance"));
                    break;
                default: throw new Exception($"Unknown ELB command: '{args[1]}'");
            }
        }
    }
}
