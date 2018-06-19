using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.ECS;
using AsmodatStandard.Threading;
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

        public Task DeregisterTaskDefinitionsAsync(IEnumerable<string> arns) => arns.ForEachAsync(
            arn => _ECSClient.DeregisterTaskDefinitionAsync(
                    new Amazon.ECS.Model.DeregisterTaskDefinitionRequest() { TaskDefinition = arn }),
                    _maxDegreeOfParalelism
            ).EnsureSuccess();


        public Task UpdateServicesAsync(IEnumerable<string> arns, int desiredCount, string cluster) => arns.ForEachAsync(
            arn => _ECSClient.UpdateServiceAsync(
                    new Amazon.ECS.Model.UpdateServiceRequest() { Service = arn, DesiredCount = desiredCount, Cluster = cluster }),
                    _maxDegreeOfParalelism
            ).EnsureSuccess();


        public Task DeleteServicesAsync(IEnumerable<string> arns, string cluster) => arns.ForEachAsync(
            arn => _ECSClient.DeleteServiceAsync(new Amazon.ECS.Model.DeleteServiceRequest() { Service = arn, Cluster = cluster }),
                    _maxDegreeOfParalelism
            ).EnsureSuccess();


        public Task StopTasksAsync(IEnumerable<string> arns, string cluster) => arns.ForEachAsync(arn =>
                _ECSClient.StopTaskAsync(
                    new Amazon.ECS.Model.StopTaskRequest() { Task = arn, Cluster = cluster }),
                    _maxDegreeOfParalelism
            ).EnsureSuccess();
    }
}
