using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECS;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;
using Amazon.Runtime;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AWSHelper.Extensions
{
    internal static class ExecutionEx
    {
        public static Exception PrintResult(this Exception ex)
        {
            Console.WriteLine($"Result: {(ex == null ? "SUCCESS" : $"FAILURE, Error: {ex.JsonSerializeAsPrettyException(Newtonsoft.Json.Formatting.Indented)}")}");
            return ex;
        }

        public static T PrintResponse<T>(this T response)
        {
            Console.WriteLine($"Result: {(response == null ? "UNKNOWN, Response: NULL" : $"SUCCESS, Response: {response.JsonSerialize(Formatting.Indented)}")}");
            return response;
        }
    }
}
