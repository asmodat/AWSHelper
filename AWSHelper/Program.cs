using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AsmodatStandard.Extensions;
using AWSHelper.CloudWatch;
using AWSHelper.ECS;
using AWSHelper.ELB;
using AWSHelper.Route53;

namespace AWSHelper
{
    class Program
    {
        private static Dictionary<string, string> GetNamedArguments(string[] args)
        {
            var namedArgs = new Dictionary<string, string>();
            foreach (var arg in args)
            {
                if (arg.Contains('='))
                {
                    var kv = arg.SplitByFirst('=');
                    var key = kv[0].Trim(' ', '-', '=');
                    var value = kv[1].Trim();
                    namedArgs[key] = value;
                }
            }
            return namedArgs;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("*** Started AWSHelper v0.2 by Asmodat ***");

            if (args.Length == 2)
                throw new Exception("At least 2 arguments must be specified.");
            
            var nArgs = GetNamedArguments(args);

            Console.WriteLine($"Executing command: '{args[0]} {args[1]}' Arguments: \n{nArgs.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}\n");

            if (args[0] == "ecs")
            {
                var helper = new ECSHelper();
                switch (args[1])
                {
                    case "destroy-service":
                        helper.DestroyService(
                            nArgs.FirstOrDefault(x => x.Key == "cluster").Value, //optional
                            nArgs["service"]).Wait();
                        ; break;
                    case "destroy-task-definitions":
                        helper.DestroyTaskDefinitions(nArgs["family"]).Wait();
                        ; break;
                    case "await-service-start":
                        helper.WaitForServiceToStart(
                            nArgs.FirstOrDefault(x => x.Key == "cluster").Value, //optional
                            nArgs["service"],
                            nArgs["timeout"].ToInt32()).Wait();
                        ; break;
                    default: throw new Exception($"Unknown ECS command: '{args[1]}'");
                }
            }
            else if (args[0] == "elb")
            {
                var helper = new ELBHelper();
                switch (args[1])
                {
                    case "destroy-load-balancer":
                        helper.DestroyLoadBalancer(nArgs["name"]).Wait();
                        ; break;
                    default: throw new Exception($"Unknown ELB command: '{args[1]}'");
                }
            }
            else if (args[0] == "cloud-watch")
            {
                var helper = new CloudWatchHelper();
                switch (args[1])
                {
                    case "destroy-log-group":
                        helper.DeleteLogGroupAsync(nArgs["name"]).Wait();
                        ; break;
                    default: throw new Exception($"Unknown CloudWatch command: '{args[1]}'");
                }
            }
            else if (args[0] == "route53")
            {

                var helper = new Route53Helper();
                switch (args[1])
                {
                    case "destroy-record":
                        helper.DestroyRecord(zoneId: nArgs["zone"], recordName: nArgs["name"], recordType: nArgs["type"]).Wait();
                        ; break;
                    default: throw new Exception($"Unknown Route53 command: '{args[1]}'");
                }
            }
            else
                throw new Exception($"Unknown command: '{args[0]}'");

            Console.WriteLine("Success");
        }
    }
}
