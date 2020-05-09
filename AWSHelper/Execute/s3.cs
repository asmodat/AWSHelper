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
using AWSWrapper.S3.Models;
using System.Threading.Tasks;

namespace AWSHelper
{
    public partial class Program
    {
        private static async Task executeS3(string[] args, Credentials credentials)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);
            var helper = new S3Helper(credentials);

            switch (args[1]?.ToLower())
            {
                case "upload-text":
                    {
                        var keyId = nArgs.GetValueOrDefault("key");
                        var result = await helper.UploadTextAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["path"],
                            text: nArgs["text"],
                            keyId: keyId,
                            encoding: Encoding.UTF8);

                        WriteLine($"SUCCESS, Text Saved, Bucket {nArgs["bucket"]}, Path {nArgs["path"]}, Encryption Key {keyId}, ETag: {result}");
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
                            var result = await helper.UploadStreamAsync(
                                bucketName: nArgs["bucket"],
                                key: nArgs["path"],
                                inputStream: stream,
                                keyId: keyId);

                            WriteLine($"SUCCESS, File was uploaded, result: {result}");
                        }
                    }
                    ; break;
                case "upload-folder":
                    {
                        var bucket = nArgs["bucket"];
                        var path = nArgs["path"];
                        var directory = nArgs["input"].ToDirectoryInfo();
                        var excludeFiles = nArgs.GetValueOrDefault("exclude-files", "")
                            .EscapedSplit(',').Where(x => !x.IsNullOrWhitespace()).Select(x => x.ToFileInfo()).ToArray();
                        var excludeDirectories = nArgs.GetValueOrDefault("exclude-directories", "")
                            .EscapedSplit(',').Where(x => !x.IsNullOrWhitespace()).Select(x => x.ToDirectoryInfo()).ToArray();
                        var excludePatterns = nArgs.GetValueOrDefault("exclude-file-patterns","")
                            .EscapedSplit(',').Where(x => !x.IsNullOrWhitespace()).ToArray();
                        var includePatterns = nArgs.GetValueOrDefault("include-file-patterns", "*")
                            .EscapedSplit(',').Where(x => !x.IsNullOrWhitespace()).ToArray();
                        var recursive = nArgs.GetValueOrDefault("recursive").ToBoolOrDefault(); //default false
                        var force = nArgs.GetValueOrDefault("force").ToBoolOrDefault(true); //upload even if exists

                        if (!directory.Exists)
                            throw new Exception($"Can't upload directory '{directory?.FullName}' because it doesn't exists.");

                        var files = directory.GetFiles(recursive: recursive,
                            inclusivePatterns: includePatterns,
                            exclusivePatterns: excludePatterns);

                        var directories = directory.GetDirectories(recursive: recursive).Merge(directory);

                        if (files.IsNullOrEmpty())
                            WriteLine($"No files were found in directory '{directory?.FullName}'");

                        if (directories.IsNullOrEmpty())
                            WriteLine($"No sub-directories were found in directory '{directory?.FullName}'");

                        var prefix = directory.FullName;
                        int uploadedFiles = 0;
                        int uploadedDirectories = 0;

                        WriteLine("Uploading Files...");
                        files.ParallelForEach(file =>
                        {
                            if(excludeFiles.Any(x => x.FullName == file.FullName) || excludeDirectories.Any(x => file.HasSubDirectory(x)))
                            {
                                WriteLine($"Skipping following File Upload due to exclude-files/directories parameter: '{file.FullName}'");
                                return;
                            }

                            var destination = (path.StartsWith("/") ? path.TrimStart("/") : path).TrimEnd("/") + "/" +
                                file.FullName.TrimStartSingle(prefix).TrimStart('/', '\\').Replace("\\", "/");
                            destination = destination.TrimStart("/"); //in case path was null or '/'

                            WriteLine($"Uploading '{file.FullName}' => '{bucket}/{destination}' ...");

                            using (var stream = file.OpenRead())
                            {
                                var result = helper.UploadStreamAsync(
                                    bucketName: bucket,
                                    key: destination,
                                    inputStream: stream,
                                    keyId: null,
                                    throwIfAlreadyExists: !force).Result;

                                ++uploadedFiles;
                                WriteLine($"SUCCESS, File '{destination}' was uploaded, result: {result}");
                            }
                        });

                        if (nArgs.GetValueOrDefault("create-directories").ToBoolOrDefault(false))
                        {
                            WriteLine("Creating Directories...");

                            directories.ParallelForEach(dir =>
                            {
                                if (excludeDirectories.Any(x => x.FullName == dir.FullName) || excludeDirectories.Any(x => dir.HasSubDirectory(x)))
                                {
                                    WriteLine($"Skipping following Directory Creation due to exclude-files/directories parameter: '{dir.FullName}'");
                                    return;
                                }

                                var destination = (path.StartsWith("/") ? path.TrimStart("/") : path).TrimEnd("/") + "/" +
                                    dir.FullName.TrimStartSingle(prefix).Trim('/', '\\').Replace("\\", "/");
                                destination = destination.TrimStart("/"); //in case path was null or '/'

                                if (destination.IsNullOrEmpty() || helper.ObjectExistsAsync(bucket, key: destination + "/").Result)
                                {
                                    WriteLine($"Directory '{destination}' already exists or is an empty destination.");
                                    return;
                                }

                                helper.CreateDirectory(bucketName: bucket, path: destination).Await();
                                ++uploadedDirectories;
                                WriteLine($"Created empty directory '{destination}'.");
                            });
                        }

                        WriteLine($"SUCCESS, uploaded {uploadedFiles} files and {uploadedDirectories} directories.");
                    }
                    ; break;
                case "download-text":
                    {
                        var result = await helper.DownloadTextAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["path"],
                            eTag: nArgs.GetValueOrDefault("etag"),
                            version: nArgs.GetValueOrDefault("version"),
                            encoding: Encoding.UTF8);

                        WriteLine($"SUCCESS, Text Read, Bucket: {nArgs["bucket"]}, Path: {nArgs.GetValueOrDefault("path")}, Version: {nArgs.GetValueOrDefault("version")}, eTag: {nArgs.GetValueOrDefault("etag")}, Read: {result?.Length ?? 0} [characters], Result:");
                        Console.WriteLine(result);
                    }
                    ; break;
                case "delete-object":
                    {
                        var bucket = nArgs["bucket"];
                        var path = nArgs["path"];
                        var result = await helper.DeleteVersionedObjectAsync(
                            bucketName: bucket,
                            key: path,
                            throwOnFailure: true);

                        if (!result && nArgs.GetValueOrDefault("throw-if-not-deleted").ToBoolOrDefault(false))
                            throw new Exception($"File was NOT deleted, Bucket: {bucket}, Path: {path}");

                        WriteLine($"SUCCESS, Text Read, Bucket: {bucket}, Path: {path}, Deleted: '{(result ? "true" : "false")}'");
                        Console.Write(result);
                    }
                    ; break;
                case "object-exists":
                    {
                        var throwIfNotFound = nArgs.GetValueOrDefault("throw-if-not-found").ToBoolOrDefault(false);
                        var exists = await helper.ObjectExistsAsync(
                            bucketName: nArgs["bucket"],
                            key: nArgs["path"]);

                        if (!exists && throwIfNotFound)
                            throw new Exception($"File Does NOT exists, Bucket: {nArgs["bucket"]}, Path: {nArgs["path"]}");

                        WriteLine($"SUCCESS, Object Exists Check, Bucket: {nArgs["bucket"]}, Path: {nArgs["path"]}, Exists: {(exists ? "true" : "false")}");
                        if (!throwIfNotFound)
                            Console.WriteLine(exists);
                    }
                    ; break;
                case "download-object":
                    {
                        var bucket = nArgs["bucket"];
                        var path = nArgs["path"];
                        var output = nArgs["output"];

                        if (Directory.Exists(output))
                            output = Path.Combine(output, path.Contains("/") ? path.SplitByLast('/')[1] : path);

                        WriteLine($"Started Download '{bucket}' -> '{output}'...");

                        var result = await helper.DownloadObjectAsync(
                            bucketName: nArgs["bucket"],
                            key: path,
                            eTag: nArgs.GetValueOrDefault("etag"),
                            version: nArgs.GetValueOrDefault("version"),
                            outputFile: output,
                            @override: nArgs.GetValueOrDefault("override").ToBoolOrDefault(false));

                        WriteLine($"SUCCESS, Text Read, Bucket: {bucket}, Path: {path}, Version: {nArgs.GetValueOrDefault("version")}, eTag: {nArgs.GetValueOrDefault("etag")}, Read: {result?.Length ?? 0} [B], Result: {result}");
                    }
                    ; break;
                case "download-folder":
                    {
                        var bucket = nArgs["bucket"];
                        var path = nArgs["path"].TrimEnd('/') + "/"; //path must contain '/'
                        var output = nArgs["output"].ToDirectoryInfo();
                        var @override = nArgs.GetValueOrDefault("override").ToBoolOrDefault();
                        var recursive = nArgs.GetValueOrDefault("recursive").ToBoolOrDefault(false);
                        var exclude = nArgs.GetValueOrDefault("exclude", "")
                            .EscapedSplit(',').Where(x => !(x?.TrimStart('/')).IsNullOrWhitespace()).Select(x => x.TrimStart('/')).ToArray();
                        
                        if (!output.Exists)
                        {
                            if (nArgs.GetValueOrDefault("create-output").ToBoolOrDefault(false))
                                output.Create();
                            else
                                throw new Exception($"Can't download S3 objects, because output directory '{output?.FullName}' does NOT exists.");
                        }

                        var list = helper.ListObjectsAsync(bucket, prefix: path == "/" ? null : path).Result;

                        if (list.IsNullOrEmpty())
                            WriteLine($"Coudn't find any object in bucket '{bucket}' with prefix '{path}'.");

                        list.ParallelForEach(o =>
                        {
                            var baseKey = o.Key.TrimStart('/').TrimStart(path.Trim('/')).TrimStart('/');

                            var excludeMatch = exclude?.FirstOrDefault(ex => baseKey.IsWildcardMatch(ex));
                            if (excludeMatch != null)
                            {
                                WriteLine($"Skipping download of object: '{o.Key}' due to exclude of '{excludeMatch}'.");
                                return;
                            }

                            var destination = Path.Combine(output.FullName, baseKey).ToFileInfo();
                            var nonRecursivefileName = o.Key.TrimStart('/').TrimStart(path);

                            if (!recursive && nonRecursivefileName.Count("/") > 0)
                            {
                                WriteLine($"Object '{o.Key}' will NOT be downloaded, because processing has non recursive mode.");
                                return;
                            }

                            if (!@override && destination.Exists)
                                throw new Exception($"Override not allowed and file already fxists: '{destination?.FullName}'");

                            if(o.Key.EndsWith("/"))
                            {
                                WriteLine($"Found Directory, not a file: '{o.BucketName}/{o.Key}', no need to download, created direcory '{destination.Directory.FullName}'.");
                                return;
                            }

                            if (!destination.Directory.Exists)
                            {
                                WriteLine($"Creating missing directory '{destination.Directory.FullName}'...");
                                destination.Directory.Create();
                            }

                            WriteLine($"Started Download '{bucket}/{o.Key}' -> '{destination?.FullName}'...");

                            var result = helper.DownloadObjectAsync(
                                bucketName: bucket,
                                key: o.Key,
                                eTag: o.ETag,
                                version: null,
                                outputFile: destination.FullName,
                                @override: @override).Result;

                            WriteLine($"SUCCESS, File '{destination.FullName}' was saved.");
                        });
                    }
                    ; break;
                case "delete-folder":
                    {
                        var bucket = nArgs["bucket"];
                        var path = nArgs["path"].Trim('/') + "/";
                        var recursive = nArgs.GetValueOrDefault("recursive").ToBoolOrDefault(false);

                        var list = await helper.ListObjectsAsync(bucket, prefix: path);

                        WriteLine($"Found '{list.Length}' objects with '{path}' prefix in bucket '{bucket}'.");

                        var rootPathCount = path.Trim('/').Count("/") + 1;
                        int counter = 0;
                        list.ParallelForEach(o =>
                        {
                            if (!recursive && o.Key.Trim('/').Count("/") > rootPathCount)
                            {
                                WriteLine($"Object '{o.Key}' will NOT be removed from bucket '{bucket}', due to NON recursive execution mode.");
                                return;
                            }

                            WriteLine($"Removing '{o.Key}' from bucket '{bucket}'...");
                            var success = helper.DeleteObjectAsync(bucketName: bucket, key: o.Key, throwOnFailure: true).Result;
                            ++counter;
                            WriteLine($"Sucesfully removed '{o.Key}' from bucket '{bucket}', response: {success}.");
                        });

                        list = await helper.ListObjectsAsync(bucket, prefix: path);
                        if (list.IsNullOrEmpty() && await helper.ObjectExistsAsync(bucket, key: path))
                        {
                            WriteLine($"Removing root directory '{path}' from bucket '{bucket}'...");
                            var success = await helper.DeleteObjectAsync(bucketName: bucket, key: path, throwOnFailure: true);
                            ++counter;
                            WriteLine($"Sucesfully removed root directory '{path}' from bucket '{bucket}', response: {success}.");
                        }

                        WriteLine($"SUCCESS, removed {counter} files.");
                    }
                    ; break;
                case "hash-download":
                    {
                        var sync = nArgs.GetOrThrow("sync")?.ToDirectoryInfo();

                        if (sync?.TryCreate() != true)
                            throw new Exception($"Sync directory '{sync?.FullName ?? "undefined"}' was not found or could not be created.");

                        var st = new SyncTarget()
                        {
                            id = nArgs.GetValueOrDefault("id", GuidEx.SlimUID()),
                            source = nArgs.GetOrThrow("source"),
                            status = nArgs.GetOrThrow("status"),
                            destination = nArgs.GetOrThrow("destination"),
                            sync = sync.FullName,
                            verbose = nArgs.GetValueOrDefault("verbose").ToBoolOrDefault(true),
                            verify = nArgs.GetValueOrDefault("verify").ToBoolOrDefault(false),
                            profile = nArgs.GetValueOrDefault("profile",""),
                            parallelism = nArgs.GetValueOrDefault("parallelism").ToIntOrDefault(2),
                            maxTimestamp = nArgs.GetValueOrDefault("maxTimestamp").ToLongOrDefault(20991230121314),
                            minTimestamp = nArgs.GetValueOrDefault("minTimestamp").ToLongOrDefault(0),
                            wipe = nArgs.GetOrThrow("wipe").ToBoolOrDefault(false),
                            compress = nArgs.GetValueOrDefault("compress").ToBoolOrDefault(false),
                            retry = nArgs.GetValueOrDefault("retry").ToIntOrDefault(5),
                            timeout = nArgs.GetValueOrDefault("timeout").ToIntOrDefault(60000),
                            type = SyncTarget.types.download,
                            throwIfSourceNotFound = nArgs.GetValueOrDefault("throwIfSourceNotFound").ToBoolOrDefault(true),
                        };

                        var s3hs = new S3HashStore(st);
                        var result = await s3hs.Process();
                        Console.Write(result.JsonSerialize());
                    }
                    ; break;
                case "hash-upload":
                    {
                        var sync = nArgs.GetOrThrow("sync")?.ToDirectoryInfo();

                        if (sync?.TryCreate() != true)
                            throw new Exception($"Sync directory '{sync?.FullName ?? "undefined"}' was not found or could not be created.");

                        var st = new SyncTarget()
                        {
                            id = nArgs.GetValueOrDefault("id", GuidEx.SlimUID()),
                            source = nArgs.GetOrThrow("source"),
                            status = nArgs.GetOrThrow("status"),
                            sync = sync.FullName,
                            destination = nArgs.GetOrThrow("destination"),
                            profile = nArgs.GetValueOrDefault("profile", ""),
                            verbose = nArgs.GetValueOrDefault("verbose").ToBoolOrDefault(true),
                            recursive = nArgs.GetOrThrow("recursive").ToBoolOrDefault(false),
                            parallelism = nArgs.GetValueOrDefault("parallelism").ToIntOrDefault(2),
                            retry = nArgs.GetValueOrDefault("retry").ToIntOrDefault(5),
                            rotation = nArgs.GetOrThrow("rotation").ToInt32(),
                            retention = nArgs.GetValueOrDefault("retention").ToIntOrDefault(1), // retention 1 second
                            compress = nArgs.GetValueOrDefault("compress").ToBoolOrDefault(false),
                            timeout = nArgs.GetValueOrDefault("timeout").ToIntOrDefault(180000), // 3 minutes
                            type = SyncTarget.types.upload,
                            throwIfSourceNotFound = nArgs.GetValueOrDefault("throwIfSourceNotFound").ToBoolOrDefault(true),
                        };

                        var s3hs = new S3HashStore(st);
                        var result = await s3hs.Process();
                        Console.Write(result.JsonSerialize());
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
                    ("object-exists", "Accepts params: bucket, path"),
                    ("hash-upload", "Accepts params: id (optional-UID), profile, sourc, status, sync, verbose (optional), parallelism (optional), maxTimestamp (optional), minTimestamp (optional), retry (optional), compress (optional:false), wipe, verify (optional:false), timeout (optional: 60000), throwIfSourceNotFound (optional: true)"),
                    ("hash-download", "Accepts params: id (optional), profile, sourc, status, sync, destination, recursive, verbose (optional), parallelism (optional), wipe, timeout (optional: 180000), retention, rotation, compress (optional:false), throwIfSourceNotFound (optional: true)"));
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
