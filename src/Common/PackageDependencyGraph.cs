using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Diagnostics;

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
        public static async Task<Dictionary<string, PackageDependencyGraph>> GenerateAllDependencyGraphsFromAssetsFileAsync(LockFile assetsFile, DependencyGraphSpec dependencyGraphSpec, GraphOptions graphOptions)
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
                PackageDependencyGraph dependencyGraph = await GenerateGraphForAGivenFramework(projectIdentity, framework, assetsFile.PackageSpec, projectPathToProjectNameMap, graphOptions.CheckVulnerabilities, graphOptions.GenerateProjectsOnly);
                TargetFrameworkInformation alias = assetsFile.PackageSpec.GetTargetFramework(framework.TargetFramework);
                aliasToDependencyGraph.Add(alias.TargetAlias, dependencyGraph);
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
        public static async Task<Dictionary<string, PackageDependencyGraph>> GenerateAllDependencyGraphsFromAssetsFileAsync(LockFile assetsFile, GraphOptions graphOptions)
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
                var dependenyGraph = await GenerateGraphForAGivenFramework(projectIdentity, framework, assetsFile.PackageSpec, new(), graphOptions.CheckVulnerabilities, projectsOnly: false);
                var alias = assetsFile.PackageSpec.GetTargetFramework(framework.TargetFramework);
                aliasToDependencyGraph.Add(alias.TargetAlias, dependenyGraph);
            }

            return aliasToDependencyGraph;
        }

        private static async Task<PackageDependencyGraph> GenerateGraphForAGivenFramework(
            DependencyNodeIdentity projectIdentity,
            LockFileTarget framework,
            PackageSpec packageSpec,
            Dictionary<string, string> projectPathToProjectNameMap,
            bool checkVulnerabilities,
            bool projectsOnly)
        {
            ArgumentNullException.ThrowIfNull(projectIdentity);
            ArgumentNullException.ThrowIfNull(framework);
            ArgumentNullException.ThrowIfNull(packageSpec);
            ArgumentNullException.ThrowIfNull(projectPathToProjectNameMap);
            PackageDependencyGraph graph = new(new PackageDependencyNode(projectIdentity));

            Dictionary<string, PackageDependencyNode> packageIdToNode = GenerateNodesForAllPackagesInGraph(framework, projectsOnly);

            if (checkVulnerabilities)
            {
                await VulnerabilityHelpers.GenerateVulnerabilitiesAsync(packageSpec, packageIdToNode, CancellationToken.None);
            }

            packageIdToNode.Add(graph.Node.Identity.Id, (PackageDependencyNode)graph.Node);

            // Populate Node to Node edges
            foreach (var package in framework.Libraries)
            {
                if (package.Type != "project" && projectsOnly)
                {
                    continue;
                }
                var currentPackageNode = packageIdToNode[package.Name];
                foreach (PackageDependency dependency in package.Dependencies)
                {
                    if (packageIdToNode.TryGetValue(dependency.Id, out PackageDependencyNode? packageDependencyNode))
                    {
                        currentPackageNode.ChildNodes.Add((packageDependencyNode, dependency.VersionRange));
                        packageDependencyNode.ParentNodes.Add((currentPackageNode, dependency.VersionRange));
                    }
                    else if (!projectsOnly)
                    {
                        throw new Exception($"Expected to find a node for {dependency.Id} but was unable to.");
                    }
                }
            }

            // Populate edge cost for direct PackageReferences
            if (!projectsOnly)
            {
                TargetFrameworkInformation targetFrameworkInformation = packageSpec.GetTargetFramework(framework.TargetFramework);
                foreach (var packageDependency in targetFrameworkInformation.Dependencies)
                {
                    PackageDependencyNode node = packageIdToNode[packageDependency.Name];
                    graph.Node.ChildNodes.Add((node, packageDependency.LibraryRange.VersionRange));
                    node.ParentNodes.Add((graph.Node, packageDependency.LibraryRange.VersionRange));
                }
            }

            // Populate edge cost for direct ProjectReferences
            ProjectRestoreMetadataFrameworkInfo restoreMetadataFramework = packageSpec.GetRestoreMetadataFramework(framework.TargetFramework);
            foreach (var projectReference in restoreMetadataFramework.ProjectReferences)
            {
                if (!projectPathToProjectNameMap.TryGetValue(projectReference.ProjectPath, out string? inferedProjectName))
                {
                    inferedProjectName = Path.GetFileNameWithoutExtension(projectReference.ProjectPath);
                }
                PackageDependencyNode node = packageIdToNode[inferedProjectName];
                VersionRange versionRange = new(node.Identity.Version);
                graph.Node.ChildNodes.Add((node, versionRange));
                node.ParentNodes.Add((graph.Node, versionRange));
            }

            return graph;

            static Dictionary<string, PackageDependencyNode> GenerateNodesForAllPackagesInGraph(LockFileTarget framework, bool projectsOnly)
            {
                var seenPackages = new Dictionary<string, PackageDependencyNode>(StringComparer.OrdinalIgnoreCase);

                foreach (LockFileTargetLibrary package in framework.Libraries)
                {
                    DependencyType dependencyType = Enum.TryParse(typeof(DependencyType), package.Type, ignoreCase: true, out object? result) ?
                        result != null ? (DependencyType)result : DependencyType.Package
                        : DependencyType.Package;
                    if (dependencyType == DependencyType.Package && projectsOnly)
                    {
                        continue;
                    }
                    PackageDependencyNode currentPackageNode = new(new DependencyNodeIdentity(package.Name, package.Version, dependencyType));
                    seenPackages.Add(package.Name, currentPackageNode);
                }

                return seenPackages;
            }
        }
    }
}
