using System;
using AWSWrapper.ELB;
using AsmodatStandard.IO;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeELB(string[] args)
        {
            var nArgs = CLIHelper.GetNamedArguments(args);

            var helper = new ELBHelper();
            switch (args[1])
            {
                case "destroy-load-balancer":
                    helper.DestroyLoadBalancer(nArgs["name"], throwIfNotFound: true).Wait();
                    ; break;
                case "help":
                case "--help":
                case "-help":
                case "-h":
                case "h":
                    HelpPrinter($"{args[0]}", "Amazon Elastic Load Balancer",
                    ("destroy-load-balancer", "Accepts params: name"));
                    break;
                default: throw new Exception($"Unknown ELB command: '{args[1]}'");
            }
        }
    }
}
