using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatchLogs;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;
using AWSHelper.Extensions;

namespace AWSHelper.CloudWatch
{
    public partial class CloudWatchHelper
    {
        private readonly int _maxDegreeOfParalelism;
        private readonly AmazonCloudWatchClient _client;
        private readonly AmazonCloudWatchLogsClient _clientLogs;

        public CloudWatchHelper(int maxDegreeOfParalelism = 8)
        {
            _maxDegreeOfParalelism = maxDegreeOfParalelism;
            _client = new AmazonCloudWatchClient();
            _clientLogs = new AmazonCloudWatchLogsClient();
        }

        public async Task DeleteLogGroupsAsync(IEnumerable<string> names)
        {
            var responses = await names.ForEachAsync(name =>
                _clientLogs.DeleteLogGroupAsync(
                    new Amazon.CloudWatchLogs.Model.DeleteLogGroupRequest() { LogGroupName = name }),
                    _maxDegreeOfParalelism
            );

            responses.EnsureSuccess();
        }
    }
}
