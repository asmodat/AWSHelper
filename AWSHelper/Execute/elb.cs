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
                    helper.DestroyLoadBalancer(nArgs["name"]).Wait();
                    ; break;
                default: throw new Exception($"Unknown ELB command: '{args[1]}'");
            }
        }
    }
}
