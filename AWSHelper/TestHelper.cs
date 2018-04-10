using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancingV2;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;
using AWSHelper.Extensions;
using System.Net.Http;
using AsmodatStandard.Types;

namespace AWSHelper
{
    public static class TestHelper
    {
        public static async Task AwaitSuccessCurlGET(string uri, int timeout, int intensity = 1000)
        {
            var tt = new TickTimeout(timeout, TickTime.Unit.ms);
            HttpResponseMessage lastResponse = null;
            do
            {
                var result = (await HttpHelper.CURL(HttpMethod.Get, uri, null));
                lastResponse = result.Response;

                if (lastResponse.StatusCode == System.Net.HttpStatusCode.OK)
                    return;

                if (tt.IsTriggered)
                    break;

                await Task.Delay(intensity);

            } while (tt.IsTriggered);

            throw new Exception($"AwaitSuccessCurlGET, status code: '{lastResponse?.StatusCode}', response: '{lastResponse?.Content?.ReadAsStringAsync()}'");
        }
    }
}
