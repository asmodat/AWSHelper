using System;
using AWSWrapper.ECR;
using AsmodatStandard.Extensions;
using System.Linq;
using AsmodatStandard.IO;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeECR(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var helper = new ECRHelper();
            switch (args[1])
            {
                case "list-images-by-tag":
                    Console.WriteLine($"{(helper.GetImagesByTag(imageTag: nArgs["imageTag"], registryId: nArgs["registryId"], repositoryName: nArgs["repositoryName"]).Result.Select(x => x.ImageId).JsonSerialize(Newtonsoft.Json.Formatting.Indented))}");
                    ; break;
                case "list-tagged-images":
                    Console.WriteLine($"{(helper.ListTaggedImages(registryId: nArgs["registryId"], repositoryName: nArgs["repositoryName"]).Result.JsonSerialize(Newtonsoft.Json.Formatting.Indented))}");
                    ; break;
                case "list-untagged-images":
                    Console.WriteLine($"{(helper.ListUntaggedImages(registryId: nArgs["registryId"], repositoryName: nArgs["repositoryName"]).Result.JsonSerialize(Newtonsoft.Json.Formatting.Indented))}");
                    ; break;
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
                    ("list-images-by-tag", "Accepts params: imageTag, imageTagNew, registryId, repositoryName"),
                    ("list-tagged-images", "Accepts params: imageTagNew, registryId, repositoryName"),
                    ("list-untagged-images", "Accepts params: imageTagNew, registryId, repositoryName"),
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
