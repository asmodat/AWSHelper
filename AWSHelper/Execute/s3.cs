using System;
using System.Text;
using Amazon.SecurityToken.Model;
using AsmodatStandard.IO;
using AsmodatStandard.Extensions.Collections;
using AWSWrapper.S3;
using AsmodatStandard.Extensions;
using System.IO;
using AsmodatStandard.Extensions.IO;
using System.Linq;
using System.Collections.Generic;
using AsmodatStandard.Extensions.Threading;

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
                        var keyId = nArgs.GetValueOrDefault("key");
                        var result = helper.UploadTextAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["path"],
                            text: nArgs["text"],
                            keyId: keyId,
                            encoding: Encoding.UTF8).Result;

                        Console.WriteLine($"SUCCESS, Text Saved, Bucket {nArgs["bucket"]}, Path {nArgs["path"]}, Encryption Key {keyId}, ETag: {result}");
                    }
                    ; break;
                case "upload-object":
                    {

                        var file = nArgs["input"].ToFileInfo();
                        if (!file.Exists)
                            throw new Exception($"Can't upload file '{file}' because it doesn't exists.");

                        using (var stream = file.OpenRead())
                        {
                            var keyId = nArgs.GetValueOrDefault("key");
                            var result = helper.UploadStreamAsync(
                                bucketName: nArgs["bucket"],
                                key: nArgs["path"],
                                inputStream: stream,
                                keyId: keyId).Result;

                            Console.WriteLine($"SUCCESS, File was uploaded, result: {result}");
                        }
                    }
                    ; break;
                case "upload-folder":
                    {

                        var bucket = nArgs["bucket"];
                        var path = nArgs["path"];
                        var directory = nArgs["input"].ToDirectoryInfo();

                        if (!directory.Exists)
                            throw new Exception($"Can't upload directory '{directory?.FullName}' because it doesn't exists.");

                        var files = directory.GetFilesRecursive();
                        var directories = directory.GetDirectoriesRecursive().Merge(directory);

                        if (files.IsNullOrEmpty())
                            Console.WriteLine($"No files were found in directory '{directory?.FullName}'");

                        if (directories.IsNullOrEmpty())
                            Console.WriteLine($"No sub-directories were found in directory '{directory?.FullName}'");

                        var prefix = directory.FullName;
                        var createdDirectories = new List<string>();

                        Console.WriteLine("Uploading Files...");
                        files.ParallelForEach(file =>
                        {
                            var destination = (path.StartsWith("/") ? path.TrimStart("/") : path).TrimEnd("/") + "/" +
                                file.FullName.TrimStartSingle(prefix).TrimStart('/', '\\').Replace("\\", "/");

                            createdDirectories.Add(destination.SplitByLast('/')[0]);

                            Console.WriteLine($"Uploading '{file.FullName}' => '{bucket}/{destination}' ...");

                            using (var stream = file.OpenRead())
                            {
                                var result = helper.UploadStreamAsync(
                                    bucketName: bucket,
                                    key: destination,
                                    inputStream: stream,
                                    keyId: null).Result;

                                Console.WriteLine($"SUCCESS, File was uploaded, result: {result}");
                            }
                        });

                        if (nArgs.GetValueOrDefault("create-directories").ToBoolOrDefault(false))
                        {
                            Console.WriteLine("Creating Directories...");

                            directories.ParallelForEach(dir =>
                            {
                                var destination = (path.StartsWith("/") ? path.TrimStart("/") : path).TrimEnd("/") + "/" +
                                    dir.FullName.TrimStartSingle(prefix).Trim('/', '\\').Replace("\\", "/");

                                
                                if (helper.ObjectExistsAsync(bucket, key: destination + "/").Result)
                                {
                                    Console.WriteLine($"Directory '{destination}' already exists.");
                                    return;
                                }

                                helper.CreateDirectory(bucketName: bucket, path: destination).Await();
                                Console.WriteLine($"Created empty directory '{destination}'.");
                            });
                        }

                        Console.WriteLine($"SUCCESS, uploaded '{files.Length}' files");
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

                        Console.WriteLine($"SUCCESS, Text Read, Bucket: {nArgs["bucket"]}, Path: {nArgs.GetValueOrDefault("path")}, Deleted: '{(result ? "true" : "false")}'");
                        Console.WriteLine(result);
                    }
                    ; break;
                case "object-exists":
                    {
                        var result = helper.ObjectExistsAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["path"]).Result;

                        if (nArgs.GetValueOrDefault("throw-if-not-found").ToBoolOrDefault(false))
                            throw new Exception($"File Does NOT exists, Bucket: {nArgs["bucket"]}, Path: {nArgs["path"]}");

                        Console.WriteLine($"SUCCESS, Object Exists Check, Bucket: {nArgs["bucket"]}, Path: {nArgs["path"]}, Exists: {(result ? "true" : "false")}");
                    }
                    ; break;
                case "download-object":
                    {
                        var bucket = nArgs["bucket"];
                        var path = nArgs["path"];
                        var output = nArgs["output"];

                        if (Directory.Exists(output))
                            output = Path.Combine(output, path.Contains("/") ? path.SplitByLast('/')[1] : path);

                        Console.WriteLine($"Started Download '{bucket}' -> '{output}'...");

                        var result = helper.DownloadObjectAsync(
                            bucketName: nArgs["bucket"],
                            key: path,
                            eTag: nArgs.GetValueOrDefault("etag"),
                            version: nArgs.GetValueOrDefault("version"),
                            outputFile: output,
                            @override: nArgs.GetValueOrDefault("override").ToBoolOrDefault(false)).Result;

                        Console.WriteLine($"SUCCESS, Text Read, Bucket: {bucket}, Path: {path}, Version: {nArgs.GetValueOrDefault("version")}, eTag: {nArgs.GetValueOrDefault("etag")}, Read: {result?.Length ?? 0} [B], Result: {result}");
                    }
                    ; break;
                case "download-folder":
                    {
                        var bucket = nArgs["bucket"];
                        var path = nArgs["path"];
                        var output = nArgs["output"].ToDirectoryInfo();
                        var @override = nArgs.GetValueOrDefault("override").ToBoolOrDefault();

                        if (!output.Exists)
                        {
                            if (nArgs.GetValueOrDefault("create-output").ToBoolOrDefault(false))
                                output.Create();
                            else
                                throw new Exception($"Can't download S3 objects, because output directory '{output?.FullName}' does NOT exists.");
                        }

                        var list = helper.ListObjectsAsync(bucket, prefix: path).Result;

                        if (list.IsNullOrEmpty())
                            Console.WriteLine($"Coudn't find any object in bucket '{bucket}' with prefix '{path}'.");

                        list.ParallelForEach(o =>
                        {
                            var destination = Path.Combine(output.FullName, o.Key).ToFileInfo();

                            if (!@override && destination.Exists)
                                throw new Exception($"Override not allowed and file already fxists: '{destination?.FullName}'");

                            if (!destination.Directory.Exists)
                                destination.Directory.Create();

                            if(o.Key.EndsWith("/"))
                            {
                                Console.WriteLine($"Found Directory, not a file: '{o.BucketName}/{o.Key}', no need to download, created direcory '{destination.Directory.FullName}'.");
                                return;
                            }

                            Console.WriteLine($"Started Download '{bucket}/{o.Key}' -> '{destination?.FullName}'...");

                            var result = helper.DownloadObjectAsync(
                                bucketName: bucket,
                                key: o.Key,
                                eTag: o.ETag,
                                version: null,
                                outputFile: destination.FullName,
                                @override: @override).Result;

                            Console.WriteLine($"SUCCESS, File '{destination.FullName}' was saved.");
                        });
                    }
                    ; break;
                case "delete-folder":
                    {
                        var bucket = nArgs["bucket"];
                        var path = nArgs["path"].Trim('/') + "/";
                        var recursive = nArgs.GetValueOrDefault("recursive").ToBoolOrDefault(false);

                        var list = helper.ListObjectsAsync(bucket, prefix: path).Result;

                        Console.WriteLine($"Found '{list.Length}' objects with '{path}' prefix in bucket '{bucket}'.");

                        var rootPathCount = path.Trim('/').Count("/") + 1;
                        int counter = 0;
                        list.ParallelForEach(o =>
                        {
                            if (!recursive && o.Key.Trim('/').Count("/") > rootPathCount)
                            {
                                Console.WriteLine($"Object '{o.Key}' will NOT be removed from bucket '{bucket}', due to NON recursive execution mode.");
                                return;
                            }

                            Console.WriteLine($"Removing '{o.Key}' from bucket '{bucket}'...");
                            var success = helper.DeleteObjectAsync(bucketName: bucket, key: o.Key, throwOnFailure: true).Result;
                            ++counter;
                            Console.WriteLine($"Sucesfully removed '{o.Key}' from bucket '{bucket}', response: {success}.");
                        });

                        list = helper.ListObjectsAsync(bucket, prefix: path).Result;
                        if (list.IsNullOrEmpty() && helper.ObjectExistsAsync(bucket, key: path).Result)
                        {
                            Console.WriteLine($"Removing root directory '{path}' from bucket '{bucket}'...");
                            var success = helper.DeleteObjectAsync(bucketName: bucket, key: path, throwOnFailure: true).Result;
                            ++counter;
                            Console.WriteLine($"Sucesfully removed root directory '{path}' from bucket '{bucket}', response: {success}.");
                        }

                        Console.WriteLine($"SUCCESS, removed {counter} files.");
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
