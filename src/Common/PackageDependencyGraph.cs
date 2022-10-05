using Logging;
using Microsoft.Extensions.Logging;
using NuGet.Commands;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Diagnostics;
using System.Threading;

namespace Common
{
    [DebuggerDisplay("{Node}")]
    public class PackageDependencyGraph : Graph<DependencyNodeIdentity, VersionRange>
    {
        public PackageDependencyGraph(Node<DependencyNodeIdentity, VersionRange> node) : base(node)
        {
        }

        /// <summary>
        /// Generate a graph given an assets file
        /// </summary>
        /// <param name="assetsFile">The assets file must not be null.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If the assets file is not valid</exception>
        /// <exception cref="ArgumentNullException">If the assets file is null</exception>
        public static Dictionary<string, PackageDependencyGraph> GenerateAllDependencyGraphsFromAssetsFile(LockFile assetsFile, DependencyGraphSpec dependencyGraphSpec, bool checkSourcesForVulnerabilities = false)
        {
            ArgumentNullException.ThrowIfNull(assetsFile);
            DependencyNodeIdentity projectIdentity = new(assetsFile.PackageSpec.Name, assetsFile.PackageSpec.Version, DependencyType.Project);

            List<LockFileTarget> frameworks = assetsFile.Targets.Where(e => string.IsNullOrEmpty(e.RuntimeIdentifier)).ToList();

            if (frameworks.Count == 0)
            {
                throw new InvalidProgramException("There are no valid frameworks to process in the assets file");
            }

            Dictionary<string, PackageDependencyGraph> aliasToDependencyGraph = new();
            Dictionary<string, string> projectPathToProjectNameMap = new();
            if (dependencyGraphSpec != null)
            {
                foreach (var project in dependencyGraphSpec.Projects)
                {
                    projectPathToProjectNameMap.Add(project.FilePath, project.Name);
                }
            }

            foreach (var framework in frameworks)
            {
                var dependenyGraph = GenerateGraphForAGivenFramework(projectIdentity, framework, assetsFile.PackageSpec, projectPathToProjectNameMap, checkSourcesForVulnerabilities);
                var alias = assetsFile.PackageSpec.GetTargetFramework(framework.TargetFramework);
                aliasToDependencyGraph.Add(alias.TargetAlias, dependenyGraph);
            }

            return aliasToDependencyGraph;
        }

        /// <summary>
        /// Generate a graph given an assets file
        /// </summary>
        /// <param name="assetsFile">The assets file must not be null.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If the assets file is not valid</exception>
        /// <exception cref="ArgumentNullException">If the assets file is null</exception>
        public static Dictionary<string, PackageDependencyGraph> GenerateAllDependencyGraphsFromAssetsFile(LockFile assetsFile, bool checkSourcesForVulnerabilities = false)
        {
            ArgumentNullException.ThrowIfNull(assetsFile);
            DependencyNodeIdentity projectIdentity = new(assetsFile.PackageSpec.Name, assetsFile.PackageSpec.Version, DependencyType.Project);

            List<LockFileTarget> frameworks = assetsFile.Targets.Where(e => string.IsNullOrEmpty(e.RuntimeIdentifier)).ToList();

            if (frameworks.Count == 0)
            {
                throw new InvalidProgramException("There are no valid frameworks to process in the assets file");
            }

            Dictionary<string, PackageDependencyGraph> aliasToDependencyGraph = new();

            foreach (var framework in frameworks)
            {
                var dependenyGraph = GenerateGraphForAGivenFramework(projectIdentity, framework, assetsFile.PackageSpec, new(), checkSourcesForVulnerabilities);
                var alias = assetsFile.PackageSpec.GetTargetFramework(framework.TargetFramework);
                aliasToDependencyGraph.Add(alias.TargetAlias, dependenyGraph);
            }

            return aliasToDependencyGraph;
        }

        private static PackageDependencyGraph GenerateGraphForAGivenFramework(DependencyNodeIdentity projectIdentity, LockFileTarget framework, PackageSpec packageSpec, Dictionary<string, string> projectPathToProjectNameMap, bool checkVulnerabilities)
        {
            ArgumentNullException.ThrowIfNull(projectIdentity);
            ArgumentNullException.ThrowIfNull(framework);
            ArgumentNullException.ThrowIfNull(packageSpec);
            ArgumentNullException.ThrowIfNull(projectPathToProjectNameMap);
            PackageDependencyGraph graph = new(new PackageDependencyNode(projectIdentity));

            Dictionary<string, PackageDependencyNode> packageIdToNode = GenerateNodesForAllPackagesInGraph(framework);


            if (checkVulnerabilities)
            {
                AppLogger.Logger.Log(LogLevel.Information, "Visualizing vulnerabilities for the package graph.");
                Thread.Sleep(1500);
                foreach (var package in packageIdToNode)
                {
                    var packageIdentity = (PackageIdentity)package.Value.Identity;
                    if (IsVulnerable(packageIdentity))
                    {
                        package.Value.Identity.Vulnerable = true;
                    }

                }
            }

            packageIdToNode.Add(graph.Node.Identity.Id, (PackageDependencyNode)graph.Node);

            // Populate Node to Node edges
            foreach (var package in framework.Libraries)
            {
                var currentPackageNode = packageIdToNode[package.Name];
                foreach (PackageDependency dependency in package.Dependencies)
                {
                    PackageDependencyNode packageDependencyNode = packageIdToNode[dependency.Id];

                    currentPackageNode.ChildNodes.Add((packageDependencyNode, dependency.VersionRange));
                    packageDependencyNode.ParentNodes.Add((currentPackageNode, dependency.VersionRange));
                }
            }

            // Populate edge cost for direct PackageReferences
            TargetFrameworkInformation targetFrameworkInformation = packageSpec.GetTargetFramework(framework.TargetFramework);
            foreach (var packageDependency in targetFrameworkInformation.Dependencies)
            {
                PackageDependencyNode node = packageIdToNode[packageDependency.Name];
                graph.Node.ChildNodes.Add((node, packageDependency.LibraryRange.VersionRange));
                node.ParentNodes.Add((graph.Node, packageDependency.LibraryRange.VersionRange));
            }

            // Populate edge cost for direct ProjectReferences
            ProjectRestoreMetadataFrameworkInfo restoreMetadataFramework = packageSpec.GetRestoreMetadataFramework(framework.TargetFramework);
            foreach (var projectReference in restoreMetadataFramework.ProjectReferences)
            {
                if (!projectPathToProjectNameMap.TryGetValue(projectReference.ProjectPath, out string? inferedProjectName))
                {
                    inferedProjectName = Path.GetFileNameWithoutExtension(projectReference.ProjectPath);
                }
                PackageDependencyNode node = packageIdToNode[inferedProjectName]; // TODO NK - Add logging here.
                VersionRange versionRange = new(node.Identity.Version);
                graph.Node.ChildNodes.Add((node, versionRange));
                node.ParentNodes.Add((graph.Node, versionRange));
            }

            return graph;

            static Dictionary<string, PackageDependencyNode> GenerateNodesForAllPackagesInGraph(LockFileTarget framework)
            {
                var seenPackages = new Dictionary<string, PackageDependencyNode>(StringComparer.OrdinalIgnoreCase);

                foreach (LockFileTargetLibrary package in framework.Libraries)
                {
                    DependencyType dependencyType = Enum.TryParse(typeof(DependencyType), package.Type, ignoreCase: true, out object? result) ?
                        result != null ? (DependencyType)result : DependencyType.Package
                        : DependencyType.Package;
                    PackageDependencyNode currentPackageNode = new(new DependencyNodeIdentity(package.Name, package.Version, dependencyType));
                    seenPackages.Add(package.Name, currentPackageNode);
                }

                return seenPackages;
            }
        }

        private static bool IsVulnerable(PackageIdentity packageIdentity)
        {
            // Hardcode it here. 
            if (packageIdentity.Id.Equals("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        private static async Task<bool> IsVulnerableAsync(PackageIdentity packageIdentity, List<PackageMetadataResource> metadataResources, Dictionary<PackageIdentity, bool> vulnerabilitiesCache)
        {
            if (!metadataResources.Any())
            {
                return false;
            }

            if (vulnerabilitiesCache.TryGetValue(packageIdentity, out bool result))
            {
                return result;
            }

            var vulnerable = false;
            await Parallel.ForEachAsync(metadataResources, async (resource, cancellationToken) =>
            {
                var metadata = await resource.GetMetadataAsync(packageIdentity, new SourceCacheContext(), NuGet.Common.NullLogger.Instance, cancellationToken);
                if (metadata != null)
                {
                    vulnerable |= metadata.Vulnerabilities.Any();
                }
            });

            vulnerabilitiesCache.Add(packageIdentity, vulnerable);
            return vulnerable;
        }

        private static async Task<List<PackageMetadataResource>> GetResources(Dictionary<PackageSource, SourceRepository> sourceRepositories)
        {
            var resources = new List<PackageMetadataResource>() { };
            foreach (var repository in sourceRepositories)
            {
                var resource = await repository.Value.GetResourceAsync<PackageMetadataResource>();
                resources.Add(resource);
            }
            return resources;
        }

        private static Dictionary<PackageSource, SourceRepository> GetHTTPSourceRepositories(DependencyGraphSpec dependencyGraphSpec)
        {
            using var settingsLoadContext = new SettingsLoadingContext();

            Dictionary<PackageSource, SourceRepository> sourceRepositoryCache = new();
            var projectPackageSpec = dependencyGraphSpec.GetProjectSpec(dependencyGraphSpec.Restore.First());

            var settings = Settings.LoadImmutableSettingsGivenConfigPaths(projectPackageSpec.RestoreMetadata.ConfigFilePaths, settingsLoadContext);
            var sources = projectPackageSpec.RestoreMetadata.Sources;

            IEnumerable<Lazy<INuGetResourceProvider>> providers = Repository.Provider.GetCoreV3();

            foreach (PackageSource source in sources)
            {
                if (source.IsHttp)
                {
                    SourceRepository sourceRepository = Repository.CreateSource(providers, source, FeedType.Undefined);
                    sourceRepositoryCache[source] = sourceRepository;
                }
            }

            return sourceRepositoryCache;
        }

        internal static LockFile GetAssetsFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            return new LockFileFormat().Read(fileName); // TODO - https://github.com/nkolev92/DependencyVisualizer/issues/5
        }

    }
}
