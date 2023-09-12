using System.Diagnostics;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace Common
{
    [DebuggerDisplay("{Node}")]
    public class PackageDependencyGraph : Graph<DependencyNodeIdentity, VersionRange>
    {
        public PackageDependencyGraph(Node<DependencyNodeIdentity, VersionRange> node) : base(node)
        {
        }

        public static async Task<Dictionary<string, PackageDependencyGraph>> GenerateAllDependencyGraphsFromAssetsFileAsync(
            LockFile assetsFile,
            DependencyGraphSpec dependencyGraphSpec,
            bool projectsOnly,
            List<IPackageDependencyNodeDecorator> decorators,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(assetsFile);
            ArgumentNullException.ThrowIfNull(dependencyGraphSpec);
            ArgumentNullException.ThrowIfNull(decorators);

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
                PackageDependencyGraph dependencyGraph = await GenerateGraphForAGivenFramework(projectIdentity, framework, assetsFile.PackageSpec, projectPathToProjectNameMap, projectsOnly, decorators, cancellationToken);
                TargetFrameworkInformation alias = assetsFile.PackageSpec.GetTargetFramework(framework.TargetFramework);
                aliasToDependencyGraph.Add(alias.TargetAlias, dependencyGraph);
            }

            return aliasToDependencyGraph;
        }

        private static async Task<PackageDependencyGraph> GenerateGraphForAGivenFramework(
            DependencyNodeIdentity projectIdentity,
            LockFileTarget framework,
            PackageSpec packageSpec,
            Dictionary<string, string> projectPathToProjectNameMap,
            bool projectsOnly,
            List<IPackageDependencyNodeDecorator> decorators,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(projectIdentity);
            ArgumentNullException.ThrowIfNull(framework);
            ArgumentNullException.ThrowIfNull(packageSpec);
            ArgumentNullException.ThrowIfNull(projectPathToProjectNameMap);

            PackageDependencyGraph graph = new(new PackageDependencyNode(projectIdentity));
            Dictionary<string, PackageDependencyNode> packageIdToNode = GenerateNodesForAllPackagesInGraph(framework, projectsOnly);

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

            foreach ((string _, PackageDependencyNode packageIdNode) in packageIdToNode)
            {
                foreach (var decorator in decorators)
                {
                    await decorator.DecorateAsync(packageIdNode, cancellationToken);
                }
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