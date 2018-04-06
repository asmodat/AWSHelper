using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECS;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;

namespace AWSHelper.ECS
{
    public class ServiceInfo
    {
        public readonly string ARN;
        public readonly LaunchType LaunchType;
        public readonly string Cluster;

        public ServiceInfo(string cluster,  LaunchType launchType, string arn)
        {
            this.ARN = arn;
            this.Cluster = cluster;
            this.LaunchType = launchType;
        }
    }
}
