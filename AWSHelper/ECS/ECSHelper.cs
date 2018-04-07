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
        private readonly int _maxDegreeOfParalelism;
        private readonly AmazonECSClient _ECSClient;

        public ECSHelper(int maxDegreeOfParalelism = 8)
        {
            _maxDegreeOfParalelism = maxDegreeOfParalelism;
            _ECSClient = new AmazonECSClient();
        }

        public async Task DeregisterTaskDefinitionsAsync(IEnumerable<string> arns)
        {
            var responses = await arns.ForEachAsync(arn =>
                _ECSClient.DeregisterTaskDefinitionAsync(
                    new Amazon.ECS.Model.DeregisterTaskDefinitionRequest() { TaskDefinition = arn }),
                    _maxDegreeOfParalelism
            );

            responses.EnsureSuccess();
        }

        public async Task UpdateServicesAsync(IEnumerable<string> arns, int desiredCount, string cluster)
        {
            var responses = await arns.ForEachAsync(arn =>
                _ECSClient.UpdateServiceAsync(
                    new Amazon.ECS.Model.UpdateServiceRequest() { Service = arn, DesiredCount = desiredCount, Cluster = cluster }),
                    _maxDegreeOfParalelism
            );

            responses.EnsureSuccess();
        }

        public async Task DeleteServicesAsync(IEnumerable<string> arns, string cluster)
        {
            var responses = await arns.ForEachAsync(arn =>
                _ECSClient.DeleteServiceAsync(new Amazon.ECS.Model.DeleteServiceRequest() { Service = arn, Cluster = cluster }),
                    _maxDegreeOfParalelism
            );

            responses.EnsureSuccess();
        }

        public async Task StopTasksAsync(IEnumerable<string> arns, string cluster)
        {
            var responses = await arns.ForEachAsync(arn =>
                _ECSClient.StopTaskAsync(
                    new Amazon.ECS.Model.StopTaskRequest() { Task = arn, Cluster = cluster }),
                    _maxDegreeOfParalelism
            );

            responses.EnsureSuccess();
        }
    }
}
