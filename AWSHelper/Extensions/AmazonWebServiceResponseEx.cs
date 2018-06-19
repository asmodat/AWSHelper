using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.ECS;
using AsmodatStandard.Extensions;
using AsmodatStandard.Threading;
using AsmodatStandard.Extensions.Collections;
using Amazon.Runtime;

namespace AWSHelper.Extensions
{
    public static class AmazonWebServiceResponseEx
    {
        public static void EnsureSuccess<T>(this T[] responses, [System.Runtime.CompilerServices.CallerMemberName] string callerMemberName = "") where T : AmazonWebServiceResponse
            => EnsureSuccess(responses.ToIEnumerable(), callerMemberName);

        public static async Task EnsureSuccess<T>(this Task<T[]> responses, [System.Runtime.CompilerServices.CallerMemberName] string callerMemberName = "") where T : AmazonWebServiceResponse
            => EnsureSuccess((await responses)?.ToIEnumerable(), callerMemberName);

        public static void EnsureSuccess<T>(this IEnumerable<T> responses, [System.Runtime.CompilerServices.CallerMemberName] string callerMemberName = "") where T : AmazonWebServiceResponse
        {
            var errors = new List<Exception>();
            foreach (var response in responses)
                if(response?.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    errors.Add(new Exception($"Status code: '{response?.HttpStatusCode}', metadata: '{response?.ResponseMetadata.JsonSerialize()}'"));

            if (errors.Count > 0)
                throw new AggregateException($"'{callerMemberName}' Failed '{errors.Count}' request/s.", errors);
        }

        public static void EnsureSuccess<T>(this T response, [System.Runtime.CompilerServices.CallerMemberName] string callerMemberName = "") where T : AmazonWebServiceResponse
        {
            if (response?.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"'{callerMemberName}' Failed. Status code: '{response?.HttpStatusCode}', metadata: '{response?.ResponseMetadata.JsonSerialize()}'");
        }

        public static async Task EnsureSuccessAsync<T>(this Task<T> tResponse, [System.Runtime.CompilerServices.CallerMemberName] string callerMemberName = "") where T : AmazonWebServiceResponse
        {
            var response = await tResponse;
            if (response?.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception($"'{callerMemberName}' Failed. Status code: '{response?.HttpStatusCode}', metadata: '{response?.ResponseMetadata.JsonSerialize()}'");
        }
    }
}
