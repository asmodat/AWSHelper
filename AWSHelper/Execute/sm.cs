using System;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using AsmodatStandard.Extensions.IO;
using System.Linq;
using AsmodatStandard.IO;
using AWSWrapper.SM;
using Amazon.SecurityToken.Model;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AWSHelper
{
    public partial class Program
    {
        private static async Task<bool> executeSM(string[] args, Credentials credentials)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);
            var helper = new SMHelper(credentials);

            switch (args[1])
            {
                case "get-secret":
                    {
                        var name = nArgs["name"];
                        var key = nArgs.GetValueOrDefault("key", null);
                        var output = nArgs["output"]?.ToFileInfo();
                        var force = nArgs["force"].ToBoolOrDefault(false);

                        if (!(output?.Directory).TryCreate())
                            throw new Exception($"Failed to create output directory '{output?.Directory?.FullName ?? "undefined"}'");

                        if (output.Exists && !force)
                            throw new Exception($"Failed to create secret, output '{output.FullName}' already exists.");

                        var result = await helper.GetSecret(name: name, key: key);

                        output.WriteAllText(result);

                        WriteLine($"{result ?? "undefined"}");

                        return true;
                    }
                case "show-secret":
                    {
                        var name = nArgs["name"];
                        var key = nArgs.GetValueOrDefault("key", null);
                        var result = await helper.GetSecret(name: name, key: key);
                        Console.Write($"{result ?? "undefined"}");
                        return true;
                    }
                case "help":
                    {
                        HelpPrinter($"{args[0]}", "Secrets Manager",
                            ("get-secret", "Accepts params: name, key (optional), output, silent, force"),
                            ("show-secret", "Accepts params: name, key (optional)"));
                        return true;
                    }
                default:
                    {
                        Console.WriteLine($"Try '{args[0]} help' to find out list of available commands.");
                        throw new Exception($"Unknown IAM command: '{args[0]} {args[1]}'");
                    }
            }
        }
    }
}
