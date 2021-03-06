﻿using System;
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
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using AsmodatStandard.Extensions.Net;
using Amazon.Route53.Model;
using AWSWrapper.ACM;

namespace AWSHelper.Fargate
{
    public static partial class FargateResourceHelperV2
    {
        public static void Create(
            FargateResourceV2 resource,
            ELBHelper elb, Route53Helper e53, ECSHelper ecs, CloudWatchHelper cw, KMSHelper kms, IAMHelper iam, ACMHelper acm)
        {
            var errList = new List<Exception>();

            Console.WriteLine("Crating S3 Access Policy...");
            var policyS3 = iam.CreatePolicyS3Async(
                name: resource.PolicyNameAccessS3,
                paths: resource.PathsS3,
                permissions: resource.PermissionsS3,
                description: $"S3 Access Policy '{resource.PolicyNameAccessS3}' to '{resource.PathsS3.JsonSerialize()}' auto generated by AWSHelper").Result.PrintResponse();

            Console.WriteLine($"Crating Execution Role '{resource.RoleName}'...");
            var roleEcs = iam.CreateRoleWithPoliciesAsync(
                roleName: resource.RoleName,
                policies: new string[] { resource.ExecutionPolicy, resource.PolicyNameAccessS3 },
                roleDescription: $"Role '{resource.RoleName}' auto generated by AWSHelper").Result.PrintResponse();

            Console.WriteLine($"Awaiting {resource.RoleCreateAwaitDelay / 1000} [s] to ensure that role was indexed...");
            Thread.Sleep(resource.RoleCreateAwaitDelay);

            Console.WriteLine($"Crating Default S3 Storage Grant '{resource.StorageGrantDefaultS3}' created for role '{resource.RoleName}'...");
            var defaultGrantResult = kms.CreateRoleGrantByName(
             keyName: resource.StorageKeyDefaultS3,
             grantName: resource.StorageGrantDefaultS3,
             roleName: resource.RoleName,
             grant: KMSHelper.GrantType.EncryptDecrypt).Result.PrintResponse();

            Console.WriteLine($"Crating Internal S3 Storage Grant '{resource.StorageGrantInternalS3}' created for role '{resource.RoleName}'...");
            var internalGrantResult = kms.CreateRoleGrantByName(
             keyName: resource.StorageKeyInternalS3,
             grantName: resource.StorageGrantInternalS3,
             roleName: resource.RoleName,
             grant: KMSHelper.GrantType.EncryptDecrypt).Result.PrintResponse();

            Console.WriteLine("Crating Application Load Balancer...");
            var loadBalancer = elb.CreateApplicationLoadBalancerAsync(resource.LoadBalancerName, resource.Subnets, resource.SecurityGroups, !resource.IsPublic).Result.PrintResponse();

            Console.WriteLine("Retriving Certificate...");
            var cert = acm.DescribeCertificateByDomainName(resource.CertificateDomainName).Result.PrintResponse();

            Console.WriteLine("Creating HTTP Target Group...");
            var targetGroup_http = elb.CreateHttpTargetGroupAsync(resource.TargetGroupName, resource.Port, resource.VPC, resource.HealthCheckPath).Result.PrintResponse();

            Console.WriteLine("Creating HTTPS Listener...");
            var listener_https = elb.CreateHttpsListenerAsync(loadBalancer.LoadBalancerArn, targetGroup_http.TargetGroupArn, certificateArn: cert.CertificateArn).Result.PrintResponse();

            Console.WriteLine("Creating HTTP Listener...");
            var listener_http = elb.CreateHttpListenerAsync(loadBalancer.LoadBalancerArn, targetGroup_http.TargetGroupArn, resource.Port).Result.PrintResponse();

            if (resource.IsPublic && !resource.ZonePublic.IsNullOrWhitespace())
            {
                Console.WriteLine("Creating Route53 DNS Record for the public zone...");
                e53.UpsertCNameRecordAsync(
                    resource.ZonePublic,
                    name: resource.DNSCName,
                    value: loadBalancer.DNSName,
                    ttl: 60).Await();
            }

            if (!resource.ZonePrivate.IsNullOrWhitespace())
            {
                Console.WriteLine("Creating Route53 DNS Record for the private zone...");
                e53.UpsertCNameRecordAsync(
                    resource.ZonePrivate,
                    name: resource.DNSCName,
                    value: loadBalancer.DNSName,
                    ttl: 60).Await();
            }

            Console.WriteLine("Initializeing Cluster...");
            var createClusterResponse = ecs.CreateClusterAsync(resource.ClusterName).Result.PrintResponse();

            Console.WriteLine("Creating Log Group...");
            cw.CreateLogGroupAsync(resource.LogGroupName).Await();

            Console.WriteLine("Creating Task Definitions...");
            var taskDefinition = ecs.RegisterFargateTaskAsync(
                executionRoleArn: resource.RoleName,
                family: resource.TaskFamily,
                cpu: resource.CPU,
                memory: resource.Memory,
                name: resource.TaskDefinitionName,
                image: resource.Image,
                envVariables: resource.Environment,
                logGroup: resource.LogGroupName,
                ports: resource.Ports).Result.PrintResponse();

            Console.WriteLine("Creating Service...");
            var service = ecs.CreateFargateServiceAsync(
                name: resource.ServiceName,
                taskDefinition: taskDefinition,
                desiredCount: resource.DesiredCount,
                cluster: resource.ClusterName,
                targetGroup: targetGroup_http,
                assignPublicIP: resource.IsPublic,
                securityGroups: resource.SecurityGroups,
                subnets: resource.Subnets
                ).Result.PrintResponse();

            Console.WriteLine($"Creating Cloud Watch Metric '{resource.ELBHealthyMetricAlarmName}'...");
            var metricAlarm = cw.UpsertAELBMetricAlarmAsync(elb,
                name: resource.ELBHealthyMetricAlarmName,
                loadBalancer: resource.LoadBalancerName, targetGroup: resource.TargetGroupName,
                metric: CloudWatchHelper.ELBMetricName.HealthyHostCount,
                comparisonOperator: Amazon.CloudWatch.ComparisonOperator.LessThanThreshold,
                treshold: 1).Result.PrintResponse();
        }

        public static void SwapRoutes(FargateResourceV2 resource, 
            FargateResourceV2 resourceNew, ELBHelper elb, Route53Helper r53, CloudWatchHelper cw)
        {
            Console.WriteLine("Fetching DNS Private Record...");
            var newPrivateRecord = r53.GetCNameRecordSet(resourceNew.ZonePrivate, resourceNew.DNSCName, throwIfNotFound: true)
                .Result.PrintResponse();

            ResourceRecordSet newPublicRecord = null;

            Console.WriteLine("Updating Private Route53 Record Set (SECONDARY)...");
            r53.UpsertCNameRecordAsync(resourceNew.ZonePrivate, resource.DNSCName, newPrivateRecord.ResourceRecords.Single().Value, ttl: resourceNew.TTL,
                failover: "SECONDARY", setIdentifier: "SECONDARY-PRIVATE").Result.PrintResponse();

            if (resourceNew.IsPublic)
            {
                Console.WriteLine("Fetching DNS Public Record...");
                newPublicRecord = r53.GetCNameRecordSet(resourceNew.ZonePublic, resourceNew.DNSCName, throwIfNotFound: resourceNew.IsPublic)
                    .Result.PrintResponse();

                Console.WriteLine("Updating Public Route53 Record Set (SECONDARY)...");
                r53.UpsertCNameRecordAsync(resourceNew.ZonePublic, resource.DNSCName, newPublicRecord.ResourceRecords.Single().Value, ttl: resourceNew.TTL,
                    failover: "SECONDARY", setIdentifier: "SECONDARY-PUBLIC").Result.PrintResponse();
            }

            var recordsExist = false;
            ResourceRecordSet rrsPrivate = null;
            ResourceRecordSet rrsPublic = null;
            HealthCheck healthCheck = null;

            Console.WriteLine($"Veryfying PRIMARY Record and Health Check...");
            if ((rrsPrivate = r53.GetCNameRecordSet(resource.ZonePrivate, resource.DNSCName, failover: "PRIMARY", throwIfNotFound: false).Result) != null &&
                (healthCheck = r53.GetHealthCheckAsync(rrsPrivate.HealthCheckId, throwIfNotFound: false).Result) != null)
            {
                if (resource.IsPublic &&
                   (rrsPublic = r53.GetCNameRecordSet(resource.ZonePublic, resource.DNSCName, failover: "PRIMARY", throwIfNotFound: false).Result) != null &&
                    rrsPublic.HealthCheckId == healthCheck.Id)
                {
                    recordsExist = true;
                }
                else
                    recordsExist = true;
            }
            Console.WriteLine($"DNS PRIMARY Record and Health Check were {(recordsExist ? "" : "NOT ")}present.");

            Console.WriteLine($"Awaiting Desired State of the Cloud Watch Metric Alarm '{resourceNew.ELBHealthyMetricAlarmName}'...");
            cw.WaitForMetricState(resourceNew.ELBHealthyMetricAlarmName, Amazon.CloudWatch.StateValue.OK, resource.HealthCheckTimeout)
                .Result.PrintResponse();

            Console.WriteLine($"Upserting Health Check...");
            healthCheck = r53.UpsertCloudWatchHealthCheckAsync(
                healthCheck?.Id ?? resource.HealthCheckName,
                alarmName: resourceNew.ELBHealthyMetricAlarmName,
                alarmRegion: resource.Region,
                throwIfNotFound: false,
                insufficientDataHealthStatus: Amazon.Route53.InsufficientDataHealthStatus.Healthy,
                inverted: true //OK as long as old health check is failed
                ).Result.PrintResponse();

            if (recordsExist)
            {
                Console.WriteLine($"Awaiting Health Check Unhealthy Status...");
                r53.WaitForHealthCheckAsync(name: healthCheck.Id, status: Route53Helper.HealthCheckStatus.Unhealthy, timeout_s: resource.HealthCheckTimeout)
                    .Result.PrintResponse();

                Console.WriteLine($"Awaiting DNS Route53 Resolution into new address...");
                AwaitDnsUpsert(resource, resourceNew, r53, resource.DnsResolveTimeout).PrintResponse();

                Console.WriteLine($"Awaiting {(resource.TTL + 1) * 2} [s] for DNS route update based on TTL...");
                Thread.Sleep((resource.TTL + 1) * 2 * 1000);
            }

            Console.WriteLine("Updating Private Route53 Record Set (PRIMARY)...");
            r53.UpsertCNameRecordAsync(resource.ZonePrivate, resource.DNSCName, newPrivateRecord.ResourceRecords.Single().Value, ttl: resourceNew.TTL,
                failover: "PRIMARY", setIdentifier: "PRIMARY-PRIVATE", healthCheckId: healthCheck.Id).Result.PrintResponse();

            if (resource.IsPublic)
            {
                Console.WriteLine("Updating Public Route53 Record Set (PRIMARY)...");
                r53.UpsertCNameRecordAsync(resource.ZonePublic, resource.DNSCName, newPublicRecord.ResourceRecords.Single().Value, ttl: resourceNew.TTL,
                failover: "PRIMARY", setIdentifier: "PRIMARY-PUBLIC", healthCheckId: healthCheck.Id).Result.PrintResponse();
            }

            Console.WriteLine($"Awaiting {resourceNew.DnsUpdateDelay} [ms] for PRIMARY DNS Record update...");
            Thread.Sleep(resourceNew.DnsUpdateDelay);

            Console.WriteLine($"Upserting Health Check...");
            healthCheck = r53.UpsertCloudWatchHealthCheckAsync(
                healthCheck?.Id,
                alarmName: resourceNew.ELBHealthyMetricAlarmName,
                alarmRegion: resource.Region,
                throwIfNotFound: false,
                insufficientDataHealthStatus: Amazon.Route53.InsufficientDataHealthStatus.Unhealthy,
                inverted: false
                ).Result.PrintResponse();

            Console.WriteLine($"Awaiting Health Check Healthy Status...");
            r53.WaitForHealthCheckAsync(name: healthCheck.Id, status: Route53Helper.HealthCheckStatus.Healthy, timeout_s: 240).Result.PrintResponse();

            Console.WriteLine($"Awaiting {(resource.TTL+1)*2} [s] for DNS route update based on TTL...");
            Thread.Sleep((resource.TTL + 1) * 2 * 1000);

            Console.WriteLine($"Ensuring DNS Route53 Resolution into new address after Health Check change...");
            AwaitDnsUpsert(resource, resourceNew, r53, resource.DnsResolveTimeout).PrintResponse();
        }

        public static void SetRoutes(FargateResourceV2 resource,
            FargateResourceV2 resourceNew, ELBHelper elb, Route53Helper r53, CloudWatchHelper cw)
        {
            Console.WriteLine("Fetching DNS Private Record...");
            var newPrivateRecord = r53.GetCNameRecordSet(resourceNew.ZonePrivate, resourceNew.DNSCName, throwIfNotFound: true)
                .Result.PrintResponse();

            Console.WriteLine($"Upserting Health Check...");
            var healthCheck = r53.UpsertCloudWatchHealthCheckAsync(
                resource.HealthCheckName,
                alarmName: resourceNew.ELBHealthyMetricAlarmName,
                alarmRegion: resource.Region,
                throwIfNotFound: false,
                insufficientDataHealthStatus: Amazon.Route53.InsufficientDataHealthStatus.Healthy,
                inverted: false
                ).Result.PrintResponse();

            ResourceRecordSet newPublicRecord = null;

            Console.WriteLine("Updating Private Route53 Record Set (PRIMARY)...");
            var t1 = r53.UpsertCNameRecordAsync(resource.ZonePrivate, resource.DNSCName, newPrivateRecord.ResourceRecords.Single().Value, ttl: resourceNew.TTL,
                failover: "PRIMARY", setIdentifier: "PRIMARY-PRIVATE", healthCheckId: healthCheck.Id);

            Console.WriteLine("Updating Private Route53 Record Set (SECONDARY)...");
            var t2 = r53.UpsertCNameRecordAsync(resourceNew.ZonePrivate, resource.DNSCName, newPrivateRecord.ResourceRecords.Single().Value, ttl: resourceNew.TTL,
                failover: "SECONDARY", setIdentifier: "SECONDARY-PRIVATE");

            if (resourceNew.IsPublic)
            {
                Console.WriteLine("Fetching DNS Public Record...");
                newPublicRecord = r53.GetCNameRecordSet(resourceNew.ZonePublic, resourceNew.DNSCName, throwIfNotFound: resourceNew.IsPublic)
                    .Result.PrintResponse();

                Console.WriteLine("Updating Public Route53 Record Set (PRIMARY)...");
                var t3 = r53.UpsertCNameRecordAsync(resource.ZonePublic, resource.DNSCName, newPublicRecord.ResourceRecords.Single().Value, ttl: resourceNew.TTL,
                failover: "PRIMARY", setIdentifier: "PRIMARY-PUBLIC", healthCheckId: healthCheck.Id);

                Console.WriteLine("Updating Public Route53 Record Set (SECONDARY)...");
                var t4 = r53.UpsertCNameRecordAsync(resourceNew.ZonePublic, resource.DNSCName, newPublicRecord.ResourceRecords.Single().Value, ttl: resourceNew.TTL,
                    failover: "SECONDARY", setIdentifier: "SECONDARY-PUBLIC");

                Task.WhenAll(t1, t3);
                Task.WhenAll(t2, t4);
            }

            t1.Await();
            t2.Await();

            Console.WriteLine($"Awaiting {(resource.TTL + 1) * 2} [s] for DNS route update based on TTL...");
            Thread.Sleep((resource.TTL + 1) * 2 * 1000);

            Console.WriteLine($"Awaiting Health Check Healthy Status...");
            r53.WaitForHealthCheckAsync(name: healthCheck.Id, status: Route53Helper.HealthCheckStatus.Healthy, timeout_s: resource.HealthCheckTimeout).Result.PrintResponse();

            Console.WriteLine($"Ensuring DNS Route53 Resolution into new address after Health Check change...");
            AwaitDnsUpsert(resource, resourceNew, r53, resource.DnsResolveTimeout).PrintResponse();
        }

        public static string AwaitDnsUpsert(FargateResourceV2 resource, FargateResourceV2 resourceNew, Route53Helper r53, int recordDnsUpdateTimeout)
        {
            var hostedZoneName = r53
                .GetHostedZoneAsync(resource.IsPublic ? resource.ZonePublic : resource.ZonePrivate)
                .Result.HostedZone.Name.TrimEnd('.');

            var uriNew = $"{resourceNew.DNSCName}.{hostedZoneName}";
            var uri = $"{resource.DNSCName}.{hostedZoneName}";
            var expectedHostName = DnsEx.GetHostName(uriNew, recordDnsUpdateTimeout/2);

            string newRecord;
            var sw = Stopwatch.StartNew();
            while ((newRecord = DnsEx.GetHostName(uri, recordDnsUpdateTimeout / 10)) != expectedHostName)
            {
                Task.Delay(1000);
                if (sw.ElapsedMilliseconds >= recordDnsUpdateTimeout)
                    throw new Exception($"Failed to update Private DNS Record Set, expected: {expectedHostName}, got: {newRecord}, elapsed: {sw.ElapsedMilliseconds}/{recordDnsUpdateTimeout}");
            }

            return newRecord;
        }

    }
}
