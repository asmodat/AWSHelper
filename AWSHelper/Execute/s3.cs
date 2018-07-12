using System;
using System.Text;
using Amazon.SecurityToken.Model;
using AsmodatStandard.IO;
using AsmodatStandard.Extensions.Collections;
using AWSWrapper.S3;
using AsmodatStandard.Extensions;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeS3(string[] args, Credentials credentials)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);
            var helper = new S3Helper(credentials);

            switch (args[1]?.ToLower())
            {
                case "upload-text":
                    {
                        var result = helper.UploadTextAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["path"],
                            text: nArgs["text"],
                            keyId: nArgs.GetValueOrDefault("key"),
                            encoding: Encoding.UTF8).Result;

                        Console.WriteLine($"SUCCESS, Text Saved, Bucket {nArgs["bucket"]}, Path {nArgs["path"]}, Encryption Key {nArgs["key"]}, ETag: {result}");
                    }
                    ; break;
                case "download-text":
                    {
                        var result = helper.DownloadTextAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["path"],
                            eTag: nArgs.GetValueOrDefault("etag"),
                            version: nArgs.GetValueOrDefault("version"),
                            encoding: Encoding.UTF8).Result;

                        Console.WriteLine($"SUCCESS, Text Read, Bucket: {nArgs["bucket"]}, Path: {nArgs.GetValueOrDefault("path")}, Version: {nArgs.GetValueOrDefault("version")}, eTag: {nArgs.GetValueOrDefault("etag")}, Read: {result?.Length ?? 0} [characters], Result:");
                        Console.WriteLine(result);
                    }
                    ; break;
                case "delete-object":
                    {
                        var result = helper.DeleteVersionedObjectAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["path"],
                            throwOnFailure: true).Result;

                        Console.WriteLine($"SUCCESS, Text Read, Bucket: {nArgs["bucket"]}, Path: {nArgs.GetValueOrDefault("path")}, Deleted: '{result.DeletedObjects?.JsonSerialize() ?? "null"}'");
                        Console.WriteLine(result);
                    }
                    ; break;
                case "object-exists":
                    {
                        var result = helper.ObjectExistsAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["path"]).Result;

                        Console.WriteLine($"SUCCESS, Object Exists Check, Bucket: {nArgs["bucket"]}, Path: {nArgs.GetValueOrDefault("path")}, Exists: {(result ? "true" : "false")}");
                    }
                    ; break;
                case "help":
                case "--help":
                case "-help":
                case "-h":
                case "h":
                    HelpPrinter($"{args[0]}", "Amazon S3",
                    ("upload-text", "Accepts params: bucket, path, key, text"),
                    ("download-text", "Accepts params: bucket, path, etag (optional), version (optional)"),
                    ("delete-object", "Accepts params: bucket, path"),
                    ("object-exists", "Accepts params: bucket, path"));
                    break;
                default:
                    {
                        Console.WriteLine($"Try '{args[0]} help' to find out list of available commands.");
                        throw new Exception($"Unknown S3 command: '{args[0]} {args[1]}'");
                    }
            }
        }
    }
}
