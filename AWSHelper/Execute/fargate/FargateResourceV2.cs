using System;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using System.Linq;
using System.Collections.Generic;

namespace AWSHelper.Fargate
{
    public class FargateResourceV2
    {
        private Dictionary<string, string> _nArgs;
        public string DeploymentGuid { get; }

        public FargateResourceV2(Dictionary<string, string> nArgs)
        {
            DeploymentGuid = Guid.NewGuid().ToString();

            _nArgs = nArgs;

            Environment = new Dictionary<string, string>(nArgs.GetValueOrDefault("environment", "").Split(',').Select(x =>
            {
                var split = x.SplitByFirst(':');
                return new KeyValuePair<string, string>(split[0], split.Length == 1 ? "" : split[1]);
            }));

            Environment.Add("DEPLOYMENT_TIMESTAMP", DateTime.UtcNow.Ticks.ToString());
            Environment.Add("DEPLOYMENT_GUID", this.DeploymentGuid);
        }

        public void SetName(string newName)
            => this.Name = newName;

        public void SetDNSCName(string newDNSCName)
            => this.DNSCName = newDNSCName;

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

        public string Name { get => _nArgs["name"]; private set => _nArgs["name"] = value; }
        public string DNSCName { get => _nArgs["cname"]; private set => _nArgs["cname"] = value; }
        public string CertificateDomainName {  get => _nArgs["certificate-domain-name"]; }
        public string Region { get => _nArgs["region"]; }
        public string Image { get => _nArgs["image"]; }
        public string ZonePublic { get => _nArgs.GetValueOrDefault("zone-public"); }
        public string ZonePrivate { get => _nArgs.GetValueOrDefault("zone-private"); }
        
        public string ExecutionPolicy { get => _nArgs["execution-policy"]; }
        public string VPC { get => _nArgs["vpc"]; }
        public string HealthCheckPath { get => _nArgs["health-check-path"]; }
        public string StorageKeyDefaultS3 { get => _nArgs["storage-key-default-s3"]; }
        public string StorageKeyInternalS3 { get => _nArgs["storage-key-internal-s3"]; }

        public string[] PathsS3 { get => _nArgs["paths-s3"].Split(',').Where(x => !x.IsNullOrWhitespace()).ToArray(); }
        public string[] Subnets { get => _nArgs["subnets"].Split(','); }
        public string[] SecurityGroups { get => _nArgs["security-groups"].Split(','); }
        public int[] Ports { get => _nArgs["ports"].Split(',').Select(x => x.ToInt32()).ToArray(); }

        public int RoleCreateAwaitDelay { get => _nArgs.GetValueOrDefault("role-create-delay-ms").ToIntOrDefault(30000); }
        public int CPU { get => _nArgs.GetValueOrDefault("cpu").ToIntOrDefault(256); }
        public int Memory { get => _nArgs.GetValueOrDefault("memory").ToIntOrDefault(512); }
        public int DesiredCount { get => _nArgs.GetValueOrDefault("desired-count").ToIntOrDefault(1); }
        public int Port { get => _nArgs["port"].ToInt32(); }
        public int TTL { get => _nArgs.GetValueOrDefault("dns-ttl").ToIntOrDefault(10); }
        public int DnsResolveTimeout { get => _nArgs.GetValueOrDefault("dns-resolve-timeout").ToIntOrDefault(5 * 60 * 1000); }
        public int DnsUpdateDelay { get => _nArgs.GetValueOrDefault("dns-update-delay").ToIntOrDefault(60 * 1000); }
        public int HealthCheckTimeout { get => _nArgs.GetValueOrDefault("chealth-check-timeout").ToIntOrDefault(5 * 60); }

        public bool IsPublic { get => _nArgs.GetValueOrDefault("public").ToBool(); }

        public IEnumerable<AWSWrapper.S3.S3Helper.Permissions> PermissionsS3
        {
            get => _nArgs["permissions-s3"].Split(',').Where(x => !x.IsNullOrWhitespace())
                .ToArray().ToEnum<AWSWrapper.S3.S3Helper.Permissions>();
        }
        public Dictionary<string, string> Environment { get; private set; }

    }
}
