using System;
using AWSWrapper.CloudWatch;
using AsmodatStandard.IO;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeCW(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var helper = new CloudWatchHelper();
            switch (args[1])
            {
                case "destroy-log-group":
                    helper.DeleteLogGroupAsync(nArgs["name"]).Wait();
                    ; break;
                default: throw new Exception($"Unknown CloudWatch command: '{args[1]}'");
            }
        }
    }
}
