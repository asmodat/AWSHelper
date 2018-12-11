using System;
using AWSWrapper.Route53;
using AsmodatStandard.IO;
using System.Collections.Generic;
using AsmodatStandard.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeR53(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var helper = new Route53Helper();
            switch (args[1])
            {
                case "destroy-record":
                    helper.DestroyRecord(
                        zoneId: nArgs["zone"], 
                        recordName: nArgs["name"], 
                        recordType: nArgs["type"], 
                        throwIfNotFound: nArgs.GetValueOrDefault("throw-if-not-foud").ToBoolOrDefault(true)).Wait();
                    ; break;
                case "get-record-sets":
                    {
                        Console.WriteLine("Loading Route53 Record Sets...");
                        var result = helper.GetRecordSets().Result;
                        Console.WriteLine("SUCCESS, Result:");
                        Console.WriteLine(result.Select(x => (x.Key.Name, x.Value.Select(y => y))).JsonSerialize(Newtonsoft.Json.Formatting.Indented));
                    }
                    ; break;
                case "list-resource-record-sets":
                    {
                        Console.WriteLine("Loading Route53 Resource Record Sets...");
                        var result = helper.ListResourceRecordSetsAsync(nArgs["zone"]).Result;

                        Console.WriteLine("SUCCESS, Result:");
                        Console.WriteLine(result.JsonSerialize(Newtonsoft.Json.Formatting.Indented));
                    }
                    ; break;
                case "help":
                case "--help":
                case "-help":
                case "-h":
                case "h":
                    HelpPrinter($"{args[0]}", "Amazon Route53",
                    ("destroy-record", "Accepts params: zone, name, type, throw-if-not-foud (optional)"),
                    ("get-record-sets", "Accepts params: no params"),
                    ("list-resource-record-sets", "Accepts params: zone"));
                    break;
                default:
                    {
                        Console.WriteLine($"Try '{args[0]} help' to find out list of available commands.");
                        throw new Exception($"Unknown Route53 command: '{args[0]} {args[1]}'");
                    }
            }
        }
    }
}
