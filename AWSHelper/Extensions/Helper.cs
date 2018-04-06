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

namespace AWSHelper.Extensions
{
    public static class Helper
    {
        public static bool IsARN(string arn)
        {
            if (arn.IsNullOrWhitespace())
                return false;

            var match = Regex.Match(arn, @"(?<=arn\:aws\:(.*)\:(.*)\:(.*)\:).*");
            if (match.Groups.Count != 4)
                return false;

            return true;
        }
        public static string GetResourceName(string arn)
        {
            if (!IsARN(arn))
                throw new Exception($"'{arn}' is not an ARN.");

            var match = Regex.Match(arn, @"(?<=arn\:aws\:(.*)\:(.*)\:(.*)\:).*");
            var result = match.Groups[0].Value;

            if (result.Contains(":") && result.Contains("/"))
                throw new Exception($"ARNs final resource '{result}' contains both ':' and '/' couldn't identify correct name.");

            if (result.Contains(":"))
                return result.SplitByFirst(':')[1];
            else if (result.Contains("/"))
                return result.SplitByFirst('/')[1];

            return result;
        }
    }
}
