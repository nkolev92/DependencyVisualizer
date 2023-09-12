﻿using Logging;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace Common
{
    public class DeprecationInfoDecorator : IPackageDependencyNodeDecorator
    {
        private readonly List<SourceRepository> _sourceRepositories;
        private readonly SourceCacheContext _sourceCacheContext;
        private readonly Dictionary<PackageIdentity, bool> PackageDeprecationData = new();
        private readonly List<PackageMetadataResource> _packageMetadataResources = new();
        private bool _packageMetadataResourcesAcquired;

        public DeprecationInfoDecorator(List<SourceRepository> sourceRepositories, SourceCacheContext sourceCacheContext)
        {
            _sourceRepositories = sourceRepositories ?? throw new ArgumentNullException(nameof(sourceRepositories));
            _sourceCacheContext = sourceCacheContext ?? throw new ArgumentNullException(nameof(sourceCacheContext));
        }

        public async Task DecorateAsync(PackageDependencyNode dependencyNode, CancellationToken cancellationToken)
        {
            if (PackageDeprecationData.TryGetValue(dependencyNode.Identity, out var isPackageDeprecated))
            {
                dependencyNode.Identity.Deprecated = isPackageDeprecated;
                return;
            }

            if (_packageMetadataResourcesAcquired)
            {
                await InitializeMetadataResource(cancellationToken);
                _packageMetadataResourcesAcquired = true;
            }

            bool isDeprecated = await IsPackageDeprecatedAsync(dependencyNode.Identity, cancellationToken);
            PackageDeprecationData.Add(dependencyNode.Identity, isDeprecated);
            dependencyNode.Identity.Deprecated = isDeprecated;
        }

        private async Task<bool> IsPackageDeprecatedAsync(PackageIdentity packageIdentity, CancellationToken cancellationToken)
        {
            List<Task<IPackageSearchMetadata>>? results = new(_packageMetadataResources.Count);

            bool isDeprecated = false;
            foreach (PackageMetadataResource packageMetadataResource in _packageMetadataResources)
            {
                var packageMetadata = packageMetadataResource.GetMetadataAsync(packageIdentity, _sourceCacheContext, NuGet.Common.NullLogger.Instance, cancellationToken);
                if (packageMetadata != null)
                {
                    results.Add(packageMetadata);
                }
            }
            await Task.WhenAll(results);
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            foreach (var result in results)
            {
                if (result?.Result != null)
                {
                    isDeprecated = isDeprecated || await result.Result.GetDeprecationMetadataAsync() != null;
                }
            }

            return isDeprecated;
        }

        private async Task InitializeMetadataResource(CancellationToken cancellationToken)
        {
            List<Task<PackageMetadataResource?>>? results = new(_sourceRepositories.Count);

            foreach (SourceRepository source in _sourceRepositories)
            {
                var metadataResource = GetMetadataResourceAsync(source, _sourceCacheContext, cancellationToken);
                if (metadataResource != null)
                {
                    results.Add(metadataResource);
                }
            }

            await Task.WhenAll(results);

            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            foreach (var result in results)
            {
                if (result?.Result != null)
                {
                    _packageMetadataResources.Add(result.Result);
                }
            }
        }

        static async Task<PackageMetadataResource?> GetMetadataResourceAsync(SourceRepository source, SourceCacheContext cacheContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PackageMetadataResource metadataResource =
                await source.GetResourceAsync<PackageMetadataResource>(cancellationToken);
            return metadataResource;
        }
    }
}