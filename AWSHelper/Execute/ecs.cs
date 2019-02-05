using System;
using System.Linq;
using AsmodatStandard.Extensions;
using AWSWrapper.ECS;
using AsmodatStandard.IO;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeECS(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var helper = new ECSHelper();
            switch (args[1])
            {
                case "destroy-cluster":
                    helper.DeleteClusterAsync(nArgs["name"]).Wait();
                    ; break;
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
                case "describe-tasks":
                    {
                        var result = helper.DescribeTasksAsync(nArgs["cluster"],nArgs["tasks"].Split(',')).Result;
                        Console.WriteLine(result.JsonSerialize(Newtonsoft.Json.Formatting.Indented));
                    }
                    ; break;
                case "help":
                case "--help":
                case "-help":
                case "-h":
                case "h":
                    HelpPrinter($"{args[0]}", "Amazon S3",
                    ("destroy-cluster", "Accepts params: name"),
                    ("destroy-service", "Accepts params: cluster, service"),
                    ("destroy-task-definitions", "Accepts params: family"),
                    ("await-service-start", "Accepts params: cluster, service, timeout"),
                    ("describe-tasks", "Accepts params: tasks (array, comma separated guid's)"));
                    break;
                default:
                    {
                        Console.WriteLine($"Try '{args[0]} help' to find out list of available commands.");
                        throw new Exception($"Unknown ECS command: '{args[0]} {args[1]}'");
                    }
            }
        }
    }
}
