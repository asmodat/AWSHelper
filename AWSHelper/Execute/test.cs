using System;
using System.Linq;
using AsmodatStandard.Extensions;
using AsmodatStandard.IO;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeCURL(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            switch (args[1])
            {
                case "curl-get":
                    TestHelper.AwaitSuccessCurlGET(
                        uri: nArgs["uri"],
                        timeout: nArgs["timeout"].ToInt32(),
                        intensity: nArgs.FirstOrDefault(x => x.Key == "intensity").Value.ToIntOrDefault(1000)).Wait();
                    ; break;
                default: throw new Exception($"Unknown test command: '{args[1]}'");
            }
        }
    }
}
