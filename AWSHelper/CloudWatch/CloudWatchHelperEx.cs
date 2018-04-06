using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECS;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;

namespace AWSHelper.CloudWatch
{
    public static class CloudWatchHelperEx
    {
        public static Task DeleteLogGroupAsync(this CloudWatchHelper cwh, string name)
            => cwh.DeleteLogGroupsAsync(new string[] { name });
    }
}
