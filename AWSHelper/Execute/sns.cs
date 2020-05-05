using System;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using AsmodatStandard.Extensions.IO;
using System.Linq;
using AsmodatStandard.IO;
using AWSWrapper.SNS;
using Amazon.SecurityToken.Model;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AWSHelper
{
    public partial class Program
    {
        private static async Task<bool> executeSNS(string[] args, Credentials credentials)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);
            var helper = new SNSHelper();

            switch (args[1])
            {
                case "send":
                    {
                        
                        return true;
                    }
                    
                case "help":
                    {
                        HelpPrinter($"{args[0]}", "Simple Notificaiton Service",
                            ("send", "Accepts params: topic, data"));
                        return true;
                    }
                default:
                    {
                        Console.WriteLine($"Try '{args[0]} help' to find out list of available commands.");
                        throw new Exception($"Unknown SNS command: '{args[0]} {args[1]}'");
                    }
            }
        }
    }
}
