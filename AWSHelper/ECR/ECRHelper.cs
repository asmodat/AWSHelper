using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ECR;
using Amazon.ECR.Model;
using AWSHelper.Extensions;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;

namespace AWSHelper.ECR
{
    public partial class ECRHelper
    {
        private readonly int _maxDegreeOfParalelism;
        internal readonly AmazonECRClient _ECRClient;

        public ECRHelper(int maxDegreeOfParalelism = 8)
        {
            _maxDegreeOfParalelism = maxDegreeOfParalelism;
            _ECRClient = new AmazonECRClient();
        }

        public async Task<BatchDeleteImageResponse> BatchDeleteImageAsync(IEnumerable<ImageIdentifier> imageIdentifiers, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (imageIdentifiers.IsNullOrEmpty())
                throw new ArgumentException($"{nameof(imageIdentifiers)} can't be null or empty.");

            var bdr = await _ECRClient.BatchDeleteImageAsync(new BatchDeleteImageRequest()
            {
                ImageIds = imageIdentifiers.ToList(),
                RegistryId = registryId,
                RepositoryName = repositoryName
            }, cancellationToken).EnsureSuccessAsync();

            if (((bdr.Failures?.Count) ?? 0) > 0)
                throw new Exception($"BatchDeleteImageAsync failed, following images were not removed sucessfully: '{bdr.Failures.JsonSerialize() ?? "null"}'");

            return bdr;
        }

        public async Task<ImageIdentifier[]> ListImagesAsync(TagStatus tagStatus, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
        {
            string nextToken = null;
            ListImagesResponse response;
            List<ImageIdentifier> ids = new List<ImageIdentifier>();
            while ((response = await _ECRClient.ListImagesAsync(new ListImagesRequest()
            {
                RegistryId = registryId,
                RepositoryName = repositoryName,
                MaxResults = 100,
                NextToken = nextToken,
                Filter = new ListImagesFilter()
                {
                    TagStatus = tagStatus
                }
            }, cancellationToken).EnsureSuccessAsync()) != null)
            {
                if ((response.ImageIds?.Count ?? 0) == 0)
                    break;

                ids.AddRange(response.ImageIds);

                if (response.NextToken.IsNullOrEmpty())
                    break;

                nextToken = response.NextToken;
            }

            return ids.ToArray();
        }

        public Task<BatchGetImageResponse> BatchGetImageByTagAsync(string imageTag, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
            => _ECRClient.BatchGetImageAsync(new BatchGetImageRequest()
            {
                RegistryId = registryId,
                RepositoryName = repositoryName,
                ImageIds = new List<ImageIdentifier>() { new ImageIdentifier() { ImageTag = imageTag } },
                AcceptedMediaTypes = new List<string>() {
                   "application/vnd.docker.distribution.manifest.v1+json",
                   "application/vnd.docker.distribution.manifest.v2+json",
                   "application/vnd.oci.image.manifest.v1+json"
               }
            }, cancellationToken).EnsureSuccessAsync();
    }
}
