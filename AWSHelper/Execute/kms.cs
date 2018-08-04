using System;
using AsmodatStandard.Extensions;
using System.Linq;
using AsmodatStandard.IO;
using AWSWrapper.KMS;
using Amazon.SecurityToken.Model;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeKMS(string[] args, Credentials credentials)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);
            var helper = new KMSHelper(credentials);

            switch (args[1])
            {
                case "list-grants":
                    {
                        var result = helper.GetGrantsByKeyNameAsync(
                         keyName: nArgs["key"],
                         grantName: nArgs["name"]).Result;

                        Console.WriteLine($"{result.JsonSerialize(Newtonsoft.Json.Formatting.Indented)}");
                    }
                    ; break;
                case "create-grant":
                    {
                        var grants = nArgs["grants"].Split(',').Where(x => !x.IsNullOrWhitespace()).ToEnum<KMSHelper.GrantType>();
                        var grant = grants.Aggregate((a, b) => a | b);

                        var result = helper.CreateRoleGrantByName(
                         keyName: nArgs["key"],
                         grantName: nArgs["name"],
                         roleName: nArgs["role"],
                         grant: grant).Result;

                        Console.WriteLine($"SUCCESS, grant '{nArgs["name"]}' of key '{nArgs["key"]}' for role '{nArgs["role"]}' was created with privileges: '{grants.Select(x => x.ToString()).JsonSerialize()}'.");
                    }
                    ; break;
                case "remove-grant":
                    {
                        var result = helper.RemoveGrantsByName(
                         keyName: nArgs["key"],
                         grantName: nArgs["name"]).Result;

                        Console.WriteLine($"SUCCESS, {result?.Length ?? 0} grant/s with name '{nArgs["name"]}' of key '{nArgs["key"]}' were removed.");
                    }
                    ; break;
                case "help": HelpPrinter($"{args[0]}", "Amazon Identity and Access Management",
                    ("list-grants", "Accepts params: key, name"),
                    ("create-grant", "Accepts params: key, name, role, grants (',' separated: Encrypt, Decrypt)"),
                    ("remove-grant", "Accepts params: key, name"));
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
