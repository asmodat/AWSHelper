using System;
using System.Collections.Generic;
using System.Linq;
using AsmodatStandard.Extensions;
using AWSHelper.CloudWatch;
using AWSHelper.ECS;
using AWSHelper.ELB;

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
            Console.WriteLine("Started AWSHelper v0.1 by Asmodat");

            if (args.Length == 0)
                throw new Exception("Arguments were not specified.");

            var nArgs = GetNamedArguments(args);

            if (args[0] == "ecs")
            {
                ECSHelper helper = new ECSHelper();

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
                    default: throw new Exception($"Unknown ECS command: '{args[1]}'");
                }
            }
            else if (args[0] == "elb")
            {
                ELBHelper helper = new ELBHelper();
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
                CloudWatchHelper helper = new CloudWatchHelper();
                switch (args[1])
                {
                    case "destroy-log-group":
                        helper.DeleteLogGroupAsync(nArgs["name"]).Wait();
                        ; break;
                    default: throw new Exception($"Unknown CloudWatch command: '{args[1]}'");
                }
            }
            else
                throw new Exception($"Unknown command: '{args[0]}'");

            Console.WriteLine("Success");
        }
    }
}
