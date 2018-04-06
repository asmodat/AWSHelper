using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECS;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;
using AWSHelper.Extensions;

namespace AWSHelper.ECS
{
    public partial class ECSHelper
    {
        public async Task<IEnumerable<string>> ListTaskDefinitionsAsync(string familyPrefix)
        {
            string token = null;
            List<string> list = new List<string>();
            Amazon.ECS.Model.ListTaskDefinitionsResponse response;
            while ((response = await _ECSClient.ListTaskDefinitionsAsync(
                new Amazon.ECS.Model.ListTaskDefinitionsRequest()
                {
                    FamilyPrefix = familyPrefix,
                    MaxResults = 100,
                    NextToken = token
                }))?.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response?.TaskDefinitionArns == null || response.TaskDefinitionArns.Count <= 0)
                    break;

                list.AddRange(response.TaskDefinitionArns);

                token = response.NextToken;
                if (token == null)
                    break;
            }

            response.EnsureSuccess();
            return list;
        }

        public async Task<IEnumerable<string>> ListClustersAsync()
        {
            string token = null;
            List<string> list = new List<string>();
            Amazon.ECS.Model.ListClustersResponse response;
            while ((response = await _ECSClient.ListClustersAsync(
                new Amazon.ECS.Model.ListClustersRequest()
                {
                    NextToken = token,
                    MaxResults = 100
                }))?.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response?.ClusterArns == null || response.ClusterArns.Count <= 0)
                    break;
                
                list.AddRange(response.ClusterArns);

                token = response.NextToken;
                if (token == null)
                    break;
            }

            response.EnsureSuccess();
            return list;
        }

        public async Task<IEnumerable<string>> ListServicesAsync(string cluster, LaunchType launchType)
        {
            string token = null;
            List<string> list = new List<string>();
            Amazon.ECS.Model.ListServicesResponse response;
            while ((response = await _ECSClient.ListServicesAsync(
                new Amazon.ECS.Model.ListServicesRequest()
                {
                   NextToken = token,
                   Cluster = cluster,
                   MaxResults = 10,
                   LaunchType = launchType
                }))?.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response?.ServiceArns == null || response.ServiceArns.Count <= 0)
                    break;

                list.AddRange(response.ServiceArns);

                token = response.NextToken;
                if (token == null)
                    break;
            }

            response.EnsureSuccess();
            return list;
        }

        public async Task<IEnumerable<string>> ListTasksAsync(string cluster, string serviceName)
        {
            string token = null;
            List<string> list = new List<string>();
            Amazon.ECS.Model.ListTasksResponse response;
            while ((response = await _ECSClient.ListTasksAsync(
                new Amazon.ECS.Model.ListTasksRequest()
                {
                    MaxResults = 100,
                    Cluster = cluster,
                    ServiceName = serviceName
                }))?.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                if (response?.TaskArns == null || response.TaskArns.Count <= 0)
                    break;

                list.AddRange(response.TaskArns);

                token = response.NextToken;
                if (token == null)
                    break;
            }

            response.EnsureSuccess();
            return list;
        }
    }
}
