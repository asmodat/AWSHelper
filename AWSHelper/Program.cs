using System;
using System.Collections.Generic;
using System.Linq;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using AsmodatStandard.Types;
using AsmodatStandard.IO;

namespace AWSHelper
{
    public partial class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] *** Started AWSHelper v0.3.2 by Asmodat ***");

            if (args.Length < 1)
            {
                Console.WriteLine("Try 'help' to find out list of available commands.");
                throw new Exception("At least 1 argument must be specified.");
            }

            var nArgs = CLIHelper.GetNamedArguments(args);

            if (args.Length > 1)
                Console.WriteLine($"Executing command: '{args[0]} {args[1]}' Arguments: \n{nArgs.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}\n");

            string executionMode;
            if (nArgs.ContainsKey("execution-mode") && 
                !(executionMode = nArgs["execution-mode"]).IsNullOrEmpty())
            {
                if (executionMode == "debug")
                {
                    Execute(args);
                }
                else if (executionMode == "silent-errors")
                {
                    try
                    {
                        Execute(args);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Failure, Error Message: {ex.JsonSerializeAsPrettyException()}");
                    }
                }
                else
                    throw new Exception($"[{TickTime.Now.ToLongDateTimeString()}] Unknown execution-mode: '{executionMode}', try: 'debug' or 'silent-errors'.");
            }
            else
            {
                try
                {
                    Execute(args);
                }
                catch
                {
                    Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Failure");
                    throw;
                }
            }

            Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Success");
        }

        private static void Execute(string[] args)
        {
            switch (args[0]?.ToLower())
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
                case "iam":
                    executeIAM(args);
                    break;
                case "s3":
                    executeS3(args);
                    break;
                case "test":
                    executeCURL(args);
                    break;
                case "help":
                case "--help":
                case "-help":
                case "-h":
                case "h":
                    HelpPrinter($"{args[0]}", "AWSHelper List of available commands",
                    ("ecs", "Accepts params: destroy-service, destroy-task-definitions, await-service-start"),
                    ("ecr", "Accepts params: retag, delete, help"),
                    ("elb", "Accepts params: destroy-load-balancer"),
                    ("cloud-watch", "Accepts params: destroy-log-group"),
                    ("route53", "Accepts params: destroy-record"),
                    ("iam", "Accepts params: create-policy, create-role, delete-policy, delete-role, help"),
                    ("s3", "Accepts params: upload-text, help"),
                    ("test", "Accepts params: curl-get"),
                    ("[flags]", "Allowed Syntax: key=value, --key=value, -key='v1 v2 v3', -k, --key"),
                    ("--execution-mode=silent-errors", "[All commands] Don't throw errors, only displays exception message."),
                    ("--execution-mode=debug", "[All commands] Throw instantly without reporting a failure."));
                    break;
                default:
                    {
                        Console.WriteLine("Try 'help' to find out list of available commands.");
                        throw new Exception($"Unknown command: '{args[0]}'.");
                    }
            }
        }

        private static void HelpPrinter(string cmd, string description, params (string param, string descritpion)[] args)
        {
            Console.WriteLine($"### HELP: {cmd}");
            Console.WriteLine($"### DESCRIPTION: ");
            Console.WriteLine($"{description}");

            if (!args.IsNullOrEmpty())
            {
                Console.WriteLine($"### OPTIONS");
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    Console.WriteLine($"### Option-{(i + 1)}: {arg.param}");

                    if (!arg.descritpion.IsNullOrEmpty())
                        Console.WriteLine($"{arg.descritpion}");
                }
            }

            Console.WriteLine($"### HELP: {cmd}");
        }
    }
}
