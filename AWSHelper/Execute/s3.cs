using System;
using System.Linq;
using System.Text;
using AsmodatStandard.IO;
using AWSWrapper.S3;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeS3(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var helper = new S3Helper();
            switch (args[1])
            {
                case "upload-text":
                    {
                        var result = helper.UploadTextAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["key"],
                            text: nArgs["text"],
                            encoding: Encoding.UTF8).Result;

                        Console.WriteLine($"SUCCESS, Text Saved, Bucket {result.BucketName}, Key {result.Key}, Version: {result.VersionId}");
                    }
                    ; break;
                case "help":
                    HelpPrinter($"{args[0]}", "Amazon S3",
                    ("upload-text", "Accepts params: bucket, key, text"));
                    break;
                default:
                    {
                        Console.WriteLine($"Try '{args[0]} help' to find out list of available commands.");
                        throw new Exception($"Unknown IAM command: '{args[0]} {args[1]}'");
                    }
            }
        }
    }
}
