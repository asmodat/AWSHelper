using System;
using AWSHelper.ECR;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeECR(string[] args)
        {
            var nArgs = GetNamedArguments(args);

            var helper = new ECRHelper();
            switch (args[1])
            {
                case "retag":
                    helper.RetagImageAsync(
                        imageTag: nArgs["imageTag"],
                        imageTagNew: nArgs["imageTagNew"],
                        registryId: nArgs["registryId"],
                        repositoryName: nArgs["repositoryName"]).Wait();
                    ; break;
                case "delete":
                    helper.BatchDeleteImagesByTag(
                        imageTag: nArgs["imageTag"],
                        registryId: nArgs["registryId"],
                        repositoryName: nArgs["repositoryName"]).Wait();
                    ; break;
                case "help": HelpPrinter($"{args[0]}", "Amazon Elastic Container Registry",
                    ("retag", "Accepts params: imageTag, imageTagNew, registryId, repositoryName"),
                    ("delete", "Accepts params: imageTag, registryId, repositoryName"));
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
