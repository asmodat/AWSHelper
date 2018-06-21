using System;
using AWSHelper.Route53;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeR53(string[] args)
        {
            var nArgs = GetNamedArguments(args);

            var helper = new Route53Helper();
            switch (args[1])
            {
                case "destroy-record":
                    helper.DestroyRecord(zoneId: nArgs["zone"], recordName: nArgs["name"], recordType: nArgs["type"]).Wait();
                    ; break;
                default: throw new Exception($"Unknown Route53 command: '{args[1]}'");
            }
        }
    }
}
