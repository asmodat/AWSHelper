using System;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using AsmodatStandard.Types;
using AsmodatStandard.IO;
using AWSWrapper.IAM;
using AWSWrapper.ST;
using Amazon.SecurityToken.Model;
using System.Threading;
using System.Diagnostics;

namespace AWSHelper
{
    public partial class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] *** Started AWSHelper v0.5.3 by Asmodat ***");

            if (args.Length < 1)
            {
                Console.WriteLine("Try 'help' to find out list of available commands.");
                throw new Exception("At least 1 argument must be specified.");
            }

            var nArgs = CLIHelper.GetNamedArguments(args);

            if (args.Length > 1)
                Console.WriteLine($"Executing command: '{args[0]} {args[1]}' Arguments: \n{nArgs.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}\n");

            var mode = nArgs.ContainsKey("execution-mode") ? nArgs["execution-mode"]?.ToLower() : null;

            ExecuteWithMode(mode, args);

            Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Success");
        }

        /// <summary>
        /// returns value indicating wheather or not execution suceeded otherwise throws
        /// </summary>
        private static bool ExecuteWithMode(string executionMode, string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);
            Console.WriteLine($"Execution mode: {executionMode ?? "not-defined"}");

            if (!executionMode.IsNullOrEmpty())
            {
                if (executionMode == "debug")
                {
                    Execute(args);
                    return true;
                }
                else if (executionMode == "silent-errors")
                {
                    try
                    {
                        Execute(args);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Failure, Error Message: {ex.JsonSerializeAsPrettyException()}");
                        return false;
                    }
                }
                else if (executionMode == "retry")
                {

                    int counter = 0;
                    var sw = Stopwatch.StartNew();
                    int times = (nArgs.ContainsKey("retry-times") ? nArgs["retry-times"] : "1").ToIntOrDefault(1);
                    int delay = (nArgs.ContainsKey("retry-delay") ? nArgs["retry-delay"] : "1000").ToIntOrDefault(1);
                    bool throws = (nArgs.ContainsKey("retry-throws") ? nArgs["retry-throws"] : "true").ToBoolOrDefault(true);
                    int timeout = (nArgs.ContainsKey("retry-timeout") ? nArgs["retry-timeout"] : $"{60 * 3600}").ToIntOrDefault(60 * 3600);

                    Console.WriteLine($"Execution with retry: Max: {times}, Delay: {delay} [ms], Throws: {(throws ? "Yes" : "No")}, Timeout: {timeout} [s]");

                    do
                    {
                        Console.WriteLine($"Execution trial: {counter}/{times}, Elapsed/Timeout: {sw.ElapsedMilliseconds/1000}/{timeout} [s]");

                        try
                        {
                            Execute(args);
                            return true;
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Failure, Error Message: {ex.JsonSerializeAsPrettyException()}");

                            if ((sw.ElapsedMilliseconds / 1000) >= timeout || (throws && counter == times))
                                throw;

                            Console.WriteLine($"Execution retry delay: {delay} [ms]");
                            Thread.Sleep(delay);
                        }
                    }
                    while (++counter <= times);

                    return true;
                }
                else
                    throw new Exception($"[{TickTime.Now.ToLongDateTimeString()}] Unknown execution-mode: '{executionMode}', try: 'debug' or 'silent-errors'.");
            }
            else
            {
                try
                {
                    Execute(args);
                    return true;
                }
                catch
                {
                    Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Failure");
                    throw;
                }
            }
        }

        private static void Execute(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            Credentials credentials = null;
            if (nArgs.ContainsKey("assume-role"))
            {
                var role = (new IAMHelper(null)).GetRoleByNameAsync(name: nArgs["assume-role"]).Result;
                var result = (new STHelper(null)).AssumeRoleAsync(role.Arn).Result;
                credentials = result.Credentials;
            }

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
                    executeIAM(args, credentials);
                    break;
                case "s3":
                    executeS3(args, credentials);
                    break;
                case "kms":
                    executeKMS(args, credentials);
                    break;
                case "fargate":
                    executeFargate(args);
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
                    ("ecs", "Accepts params: destroy-cluster, destroy-service, destroy-task-definitions, await-service-start"),
                    ("ecr", "Accepts params: retag, delete, help"),
                    ("elb", "Accepts params: destroy-load-balancer"),
                    ("cloud-watch", "Accepts params: destroy-log-group"),
                    ("route53", "Accepts params: destroy-record"),
                    ("iam", "Accepts params: create-policy, create-role, delete-policy, delete-role, help"),
                    ("s3", "Accepts params: upload-text, help"),
                    ("kms", "Accepts params: create-grant, remove-grant, help"),
                    ("fargate", "Accepts params: "),
                    ("test", "Accepts params: curl-get"),
                    ("[flags]", "Allowed Syntax: key=value, --key=value, -key='v1 v2 v3', -k, --key"),
                    ("--execution-mode=silent-errors", "[All commands] Don't throw errors, only displays exception message."),
                    ("--execution-mode=debug", "[All commands] Throw instantly without reporting a failure."),
                    ("--execution-mode=retry", "[All commands] Repeats command at least once in case it fails. [Sub Flags] retry-times (default 1), retry-delay (default 1000 ms), retry-throws (default true)"));
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
