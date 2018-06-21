using System;
using System.Linq;
using AsmodatStandard.Extensions;
using AWSHelper.ECS;

namespace AWSHelper
{
    public partial class Program
    {
        private static void executeECS(string[] args)
        {
            var nArgs = GetNamedArguments(args);

            var helper = new ECSHelper();
            switch (args[1])
            {
                case "destroy-service":
                    helper.DestroyService(
                        nArgs.FirstOrDefault(x => x.Key == "cluster").Value, //optional
                        nArgs["service"]).Wait();
                    ; break;
                case "destroy-task-definitions":
                    helper.DestroyTaskDefinitions(nArgs["family"]).Wait();
                    ; break;
                case "await-service-start":
                    helper.WaitForServiceToStart(
                        nArgs.FirstOrDefault(x => x.Key == "cluster").Value, //optional
                        nArgs["service"],
                        nArgs["timeout"].ToInt32()).Wait();
                    ; break;
                default: throw new Exception($"Unknown ECS command: '{args[1]}'");
            }
        }
    }
}
