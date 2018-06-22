using System;
using System.Threading.Tasks;
using System.Threading;
using Amazon.ECR.Model;
using Amazon.ECR;
using AsmodatStandard.Extensions;
using AsmodatStandard.Extensions.Collections;
using System.Linq;
using AsmodatStandard.Threading;
using AWSHelper.Extensions;

namespace AWSHelper.ECR
{
    public static class ECRHelperEx
    {
        public static async Task<PutImageResponse[]> RetagImageAsync(this ECRHelper ecr, string imageTag, string imageTagNew, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var images = await ecr.GetImagesByTag(imageTag, registryId, repositoryName, cancellationToken);

            var piresp = await images.ForEachAsync(i => ecr._ECRClient.PutImageAsync(new PutImageRequest()
            {
                ImageManifest = i.ImageManifest,
                ImageTag = imageTagNew,
                RegistryId = registryId,
                RepositoryName = repositoryName
            }, cancellationToken), cancellationToken: cancellationToken).EnsureSuccess();

            return piresp;
        }

        public static async Task<Image[]> GetImagesByTag(this ECRHelper ecr, string imageTag, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bi = await ecr.BatchGetImageByTagAsync(imageTag, registryId, repositoryName, cancellationToken);

            if (bi.Images.Count <= 0)
                throw new Exception($"GetImagesByTag failed, could not find any images, with '{imageTag}' tag.");

            return bi.Images.ToArray();
        }

        public static Task<ImageIdentifier[]> ListTaggedImages(this ECRHelper ecr, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
            => ecr.ListImagesAsync(TagStatus.TAGGED, registryId: registryId, repositoryName: repositoryName, cancellationToken: cancellationToken);

        public static Task<ImageIdentifier[]> ListUntaggedImages(this ECRHelper ecr, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
            => ecr.ListImagesAsync(TagStatus.UNTAGGED, registryId: registryId, repositoryName: repositoryName, cancellationToken: cancellationToken);

        public static async Task<BatchDeleteImageResponse> BatchDeleteUntaggedImages(this ECRHelper ecr, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var iIDs = await ecr.ListImagesAsync(TagStatus.UNTAGGED, registryId: registryId, repositoryName: repositoryName, cancellationToken: cancellationToken);

            if (iIDs.Length <= 0)
                return new BatchDeleteImageResponse() { HttpStatusCode = System.Net.HttpStatusCode.OK };

            return await ecr.BatchDeleteImageAsync(iIDs, registryId, repositoryName, cancellationToken);
        }

        public static async Task<BatchDeleteImageResponse> BatchDeleteImagesByTag(this ECRHelper ecr, string imageTag, string registryId, string repositoryName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (imageTag.IsNullOrEmpty())
                return await ecr.BatchDeleteUntaggedImages(registryId, repositoryName, cancellationToken);

            var images = (await ecr.GetImagesByTag(imageTag, registryId, repositoryName, cancellationToken))
                .Select(i => i.ImageId).ToArray();

            return await ecr.BatchDeleteImageAsync(images, registryId, repositoryName, cancellationToken);
        }
    }
}
