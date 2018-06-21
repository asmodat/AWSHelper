using System;
using System.Collections.Generic;
using System.Linq;
using AsmodatStandard.Extensions;
using AWSHelper.CloudWatch;
using AWSHelper.ECS;
using AWSHelper.ELB;
using AWSHelper.Route53;
using AWSHelper.ECR;

namespace AWSHelper
{
    public partial class Program
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

            switch (args[0])
            {
                case "ecs":
                    executeECS(args);
                    break;
                case "ecr":
                    executeECR(args);
                    break;
                case "elb":
                    executeELB(args);
                    break;
                case "cloud-watch":
                    executeCW(args);
                    break;
                case "route53":
                    executeR53(args);
                    break;
                case "test":
                    executeCURL(args);
                    break;
                default:
                    throw new Exception($"Unknown command: '{args[0]}'");
            }

            Console.WriteLine("Success");
        }
    }
}
