using System;
using AWSWrapper.ELB;
using AWSWrapper.Route53;
using AWSWrapper.ECS;
using AWSWrapper.CloudWatch;
using AsmodatStandard.IO;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Threading;
using AsmodatStandard.Extensions.Collections;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Amazon.SecurityToken.Model;
using AWSWrapper.KMS;
using AWSWrapper.IAM;
using Newtonsoft.Json;
using System.Threading;
using AWSHelper.Extensions;

namespace AWSHelper.Fargate
{
    public class FargateResourceV1
    {
        public string DeploymentGuid { get; }

        public FargateResourceV1()
        {
            DeploymentGuid = Guid.NewGuid().ToString();
        }

        public void SetName(string newName)
            => this.Name = newName;

        public void SetDNSCName(string newDNSCName)
            => this.DNSCName = newDNSCName;

        public void InitializeCreationParameters(Dictionary<string, string> nArgs)
        {
            Name = nArgs["name"];
            DNSCName = nArgs["cname"];
            Region = nArgs["region"];
            Image = nArgs["image"];
            TTL = nArgs.GetValueOrDefault("dns-ttl").ToIntOrDefault(10);
            DnsResolveTimeout = nArgs.GetValueOrDefault("dns-resolve-timeout").ToIntOrDefault(5*60*1000);
            DnsUpdateDelay = nArgs.GetValueOrDefault("dns-update-delay").ToIntOrDefault(60 * 1000);
            HealthCheckTimeout = nArgs.GetValueOrDefault("chealth-check-timeout").ToIntOrDefault(5 * 60); //in seconds
            ZonePublic = nArgs.GetValueOrDefault("zone-public");
            ZonePrivate = nArgs.GetValueOrDefault("zone-private");
            Subnets = nArgs["subnets"].Split(',');
            SecurityGroups = nArgs["security-groups"].Split(',');
            ExecutionPolicy = nArgs["execution-policy"];
            CPU = nArgs.GetValueOrDefault("cpu").ToIntOrDefault(256);
            Memory = nArgs.GetValueOrDefault("memory").ToIntOrDefault(512);
            DesiredCount = nArgs.GetValueOrDefault("desired-count").ToIntOrDefault(1);
            IsPublic = nArgs.GetValueOrDefault("public").ToBool();
            Port = nArgs["port"].ToInt32();
            Ports = nArgs["ports"].Split(',').Select(x => x.ToInt32()).ToArray();
            VPC = nArgs["vpc"];
            HealthCheckPath = nArgs["health-check-path"];
            StorageKeyDefaultS3 = nArgs["storage-key-default-s3"];
            StorageKeyInternalS3 = nArgs["storage-key-internal-s3"];
            RoleCreateAwaitDelay = nArgs.GetValueOrDefault("role-create-delay-ms").ToIntOrDefault(30000);
            Environment = new Dictionary<string, string>(nArgs["environment"].Split(',').Select(x =>
            {
                var split = x.SplitByFirst(':');
                return new KeyValuePair<string, string>(split[0], split.Length == 1 ? "" : split[1]);
            }));

            Environment.Add("DEPLOYMENT_TIMESTAMP", DateTime.UtcNow.Ticks.ToString());
            Environment.Add("DEPLOYMENT_GUID", this.DeploymentGuid);

            if (!nArgs.ContainsKey("paths-s3"))
                throw new NotSupportedException("paths-s3 parameter was not specified");

            PathsS3 = nArgs["paths-s3"].Split(',').Where(x => !x.IsNullOrWhitespace()).ToArray();

            PermissionsS3 = nArgs["permissions-s3"].Split(',').Where(x => !x.IsNullOrWhitespace())
                .ToArray().ToEnum<AWSWrapper.S3.S3Helper.Permissions>();

            if (PermissionsS3.IsNullOrEmpty())
                throw new Exception("No S3 permissions were found!");

            if (ZonePublic.IsNullOrWhitespace() || ZonePrivate.IsNullOrWhitespace())
                throw new Exception("Either 'zone-public' or 'zone-private' parameter must be specified.");
        }

        public void InitializeTerminationParameters(Dictionary<string, string> nArgs)
        {
            Name = nArgs["name"];
            DNSCName = nArgs["cname"];
            IsPublic = nArgs.GetValueOrDefault("public").ToBool();
            ZonePublic = nArgs.GetValueOrDefault("zone-public");
            ZonePrivate = nArgs.GetValueOrDefault("zone-private");
            StorageKeyDefaultS3 = nArgs["storage-key-default-s3"];
            StorageKeyInternalS3 = nArgs["storage-key-internal-s3"];
        }

        public string ClusterName { get => $"{Name}-ecs"; }
        public string LoadBalancerName { get => $"{Name}-alb"; }
        public string TargetGroupName { get => $"{Name}-tg"; }
        public string LogGroupName { get => $"{Name}-ecs-lg"; }
        public string TaskFamily { get => $"{Name}-ecs-tsk-fam"; }
        public string TaskDefinitionName { get => $"{Name}-ecs-tsk-def"; }
        public string PolicyNameAccessS3 { get => $"{Name}-s3-access-policy"; }
        public string RoleName { get => $"{Name}-ecs-role"; }
        public string ServiceName { get => $"{Name}-service"; }
        public string StorageGrantDefaultS3 { get => $"{Name}-s3-grant-default"; }
        public string StorageGrantInternalS3 { get => $"{Name}-s3-grant-internal"; }
        public string HealthCheckName { get => $"{Name}-hc"; }
        public string ELBHealthyMetricAlarmName { get => $"{Name}-elb-h-ma"; }

        public string Name { get; private set; }
        public string DNSCName { get; private set; }
        public string Region { get; private set; }
        public string Image { get; private set; }
        public string ZonePublic { get; private set; }
        public string ZonePrivate { get; private set; }
        
        public string ExecutionPolicy { get; private set; }
        public string VPC { get; private set; }
        public string HealthCheckPath { get; private set; }
        public string StorageKeyDefaultS3 { get; private set; }
        public string StorageKeyInternalS3 { get; private set; }

        public string[] PathsS3 { get; private set; }
        public string[] Subnets { get; private set; }
        public string[] SecurityGroups { get; private set; }
        public int[] Ports { get; private set; }

        public int RoleCreateAwaitDelay { get; private set; }
        public int CPU { get; private set; }
        public int Memory { get; private set; }
        public int DesiredCount { get; private set; }
        public int Port { get; private set; }
        public int TTL { get; private set; }
        public int DnsResolveTimeout { get; private set; }
        public int DnsUpdateDelay { get; private set; }
        public int HealthCheckTimeout { get; private set; }

        public bool IsPublic { get; private set; }

        public IEnumerable<AWSWrapper.S3.S3Helper.Permissions> PermissionsS3 { get; private set; }
        public Dictionary<string, string> Environment { get; private set; }

    }
}
