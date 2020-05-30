using System;
using System.Linq;
using System.Threading.Tasks;
using AsmodatStandard.Extensions;
using AsmodatStandard.IO;

namespace AWSHelper
{
    public partial class Program
    {
        private static async Task executeCURL(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            switch (args[1])
            {
                case "curl-get":
                    await TestHelper.AwaitSuccessCurlGET(
                        uri: nArgs["uri"],
                        timeout: nArgs["timeout"].ToInt32(),
                        intensity: nArgs.FirstOrDefault(x => x.Key == "intensity").Value.ToIntOrDefault(1000));
                    ; break;
                case "help":
                case "--help":
                case "-help":
                case "-h":
                case "h":
                    HelpPrinter($"{args[0]}", "Asmodat Tests",
                    ("curl-get", "Accepts params: uri, timeout, intensity (default: 1000 [ms])"));
                    break;
                default: throw new Exception($"Unknown test command: '{args[1]}'");
            }
        }
    }
}
