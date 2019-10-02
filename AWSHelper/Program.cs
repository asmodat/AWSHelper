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
using System.Threading.Tasks;

namespace AWSHelper
{
    public partial class Program
    {
        private static readonly string _version = "0.11.5";
        private static bool _silent = false;

        static async Task Main(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);
            _silent = nArgs.GetValueOrDefault("silent").ToBoolOrDefault(false);

            if(!_silent)
                Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] *** Started AWSHelper v{_version} by Asmodat ***");

            if (args.Length < 1)
            {
                Console.WriteLine("Try 'help' to find out list of available commands.");
                throw new Exception("At least 1 argument must be specified.");
            }

            if (!_silent && args.Length > 1)
                Console.WriteLine($"Executing command: '{args[0]} {args[1]}' Arguments: \n{nArgs.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}\n");

            var mode = nArgs.ContainsKey("execution-mode") ? nArgs["execution-mode"]?.ToLower() : null;

            await ExecuteWithMode(mode, args);

            if (!_silent)
                Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Success");
        }

        /// <summary>
        /// returns value indicating wheather or not execution suceeded otherwise throws
        /// </summary>
        private static async Task<bool> ExecuteWithMode(string executionMode, string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);
            if (!_silent)
                Console.WriteLine($"Execution mode: {executionMode ?? "not-defined"}");

            if (!executionMode.IsNullOrEmpty())
            {
                if (executionMode == "debug")
                {
                    await Execute(args);
                    return true;
                }
                else if (executionMode == "silent-errors")
                {
                    try
                    {
                        await Execute(args);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        if (!_silent)
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

                    if (!_silent)
                        Console.WriteLine($"Execution with retry: Max: {times}, Delay: {delay} [ms], Throws: {(throws ? "Yes" : "No")}, Timeout: {timeout} [s]");

                    do
                    {
                        if (!_silent)
                            Console.WriteLine($"Execution trial: {counter}/{times}, Elapsed/Timeout: {sw.ElapsedMilliseconds/1000}/{timeout} [s]");

                        try
                        {
                            await Execute(args);
                            return true;
                        }
                        catch(Exception ex)
                        {
                            if (!_silent)
                                Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Failure, Error Message: {ex.JsonSerializeAsPrettyException()}");

                            if ((sw.ElapsedMilliseconds / 1000) >= timeout || (throws && counter == times))
                                throw;

                            if (!_silent)
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
                    await Execute(args);
                    return true;
                }
                catch
                {
                    Console.WriteLine($"[{TickTime.Now.ToLongDateTimeString()}] Failure");
                    throw;
                }
            }
        }

        private static async Task Execute(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            Credentials credentials = null;
            if (nArgs.ContainsKey("assume-role"))
            {
                var role = await (new IAMHelper(null)).GetRoleByNameAsync(name: nArgs["assume-role"]);
                var result = await (new STHelper(null)).AssumeRoleAsync(role.Arn);
                credentials = result.Credentials;
            }

            switch (args[0]?.ToLower()?.TrimStart("-"))
            {
                case "ec2":
                    executeEC2(args, credentials);
                    break;
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
                    await executeS3(args, credentials);
                    break;
                case "kms":
                    executeKMS(args, credentials);
                    break;
                case "sm":
                    await executeSM(args, credentials);
                    break;
                case "fargate":
                    executeFargate(args, credentials);
                    break;
                case "test":
                    executeCURL(args);
                    break;
                case "version":
                case "ver":
                case "v":
                    Console.Write($"v{_version}");
                    break;
                case "help":
                case "h":
                    HelpPrinter($"{args[0]}", "AWSHelper List of available commands",
                    ("ec2", "Accepts params: create-instance, help"),
                    ("ecs", "Accepts params: destroy-cluster, destroy-service, destroy-task-definitions, await-service-start"),
                    ("ecr", "Accepts params: retag, delete, help"),
                    ("elb", "Accepts params: destroy-load-balancer, register-target-instance, deregister-target-instance"),
                    ("cloud-watch", "Accepts params: destroy-log-group"),
                    ("route53", "Accepts params: destroy-record, get-record-sets, list-resource-record-sets"),
                    ("iam", "Accepts params: create-policy, create-role, delete-policy, delete-role, help"),
                    ("s3", "Accepts params: upload-text, hash-upload, hash-download, help"),
                    ("kms", "Accepts params: create-grant, remove-grant, help"),
                    ("sm", "Accepts params: get-secret, show-secret"),
                    ("fargate", "Accepts params: "),
                    ("test", "Accepts params: curl-get"),
                    ("version", "Accepts params: none"),
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
