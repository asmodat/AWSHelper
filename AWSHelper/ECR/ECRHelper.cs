using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ECR;
using Amazon.ECR.Model;
using AsmodatStandard.Threading;
using AWSHelper.Extensions;
using AsmodatStandard.Extensions;

namespace AWSHelper.ECR
{
    public partial class ECRHelper
    {
        private readonly int _maxDegreeOfParalelism;
        private readonly AmazonECRClient _ECRClient;

        public ECRHelper(int maxDegreeOfParalelism = 8)
        {
            _maxDegreeOfParalelism = maxDegreeOfParalelism;
            _ECRClient = new AmazonECRClient();
        }

        public async Task<PutImageResponse[]> RetagImageAsync(string imageTag, string imageTagNew, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bi = await BatchGetImageByTagAsync(imageTag, registryId, repositoryName, cancellationToken);

            if (bi.Images.Count <= 0)
                throw new Exception($"RetagImage failed, could not find any images, with '{imageTag}' tag.");

            var piresp = await bi.Images.ForEachAsync(i => _ECRClient.PutImageAsync(new PutImageRequest()
            {
                ImageManifest = i.ImageManifest,
                ImageTag = imageTagNew,
                RegistryId = registryId,
                RepositoryName = repositoryName
            }, cancellationToken), cancellationToken: cancellationToken).EnsureSuccess();

            return piresp;
        }

        public async Task<BatchDeleteImageResponse> BatchDeleteImagesByTag(string imageTag, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (imageTag.IsNullOrEmpty())
                return await this.BatchDeleteUntaggedImages(registryId, repositoryName, cancellationToken);

            var bi = await BatchGetImageByTagAsync(imageTag, registryId, repositoryName, cancellationToken);

            if (bi.Images.Count <= 0)
                throw new Exception($"BatchDeleteImagesByTag failed, could not find any images, with '{imageTag}' tag.");

            var iIDs = bi.Images.Select(i => i.ImageId).ToList();

            var bdr = await _ECRClient.BatchDeleteImageAsync(new BatchDeleteImageRequest()
            {
                ImageIds = iIDs,
                RegistryId = registryId,
                RepositoryName = repositoryName
            }, cancellationToken).EnsureSuccessAsync();

            if (((bdr.Failures?.Count) ?? 0) > 0)
                throw new Exception($"BatchDeleteImagesByTag failed, following images were not removed sucessfully: '{bdr.Failures?.JsonSerialize() ?? "null"}'");

            return bdr;
        }

        public async Task<BatchDeleteImageResponse> BatchDeleteUntaggedImages(string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var iIDs = await ListImagesAsync(TagStatus.UNTAGGED, registryId: registryId, repositoryName: repositoryName, cancellationToken: cancellationToken);

            if (iIDs.Length <= 0)
                return new BatchDeleteImageResponse() { HttpStatusCode = System.Net.HttpStatusCode.OK };

            var bdr = await _ECRClient.BatchDeleteImageAsync(new BatchDeleteImageRequest()
            {
                ImageIds = iIDs.ToList(),
                RegistryId = registryId,
                RepositoryName = repositoryName
            }, cancellationToken).EnsureSuccessAsync();

            if (((bdr.Failures?.Count) ?? 0) > 0)
                throw new Exception($"BatchDeleteImagesByTag failed, following images were not removed sucessfully: '{bdr.Failures?.JsonSerialize() ?? "null"}'");

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
