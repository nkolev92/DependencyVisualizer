using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Versioning;
using System.Diagnostics;

namespace Common
{
    [DebuggerDisplay("{Node}")]
    public class PackageDependencyGraph : Graph<PackageIdentity, VersionRange>
    {
        public PackageDependencyGraph(Node<PackageIdentity, VersionRange> node) : base(node)
        {
        }

        /// <summary>
        /// Generate a graph given an assets file
        /// </summary>
        /// <param name="assetsFile">The assets file must not be null.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static PackageDependencyGraph FromAssetsFile(LockFile assetsFile)
        {
            ArgumentNullException.ThrowIfNull(assetsFile);
            PackageIdentity projectIdentity = new(assetsFile.PackageSpec.Name, assetsFile.PackageSpec.Version);

            List<LockFileTarget> frameworks = assetsFile.Targets.Where(e => string.IsNullOrEmpty(e.RuntimeIdentifier)).ToList();

            if (frameworks.Count == 0)
            {
                throw new InvalidProgramException("There are no valid frameworks to process in the assets file");
            }

            return GenerateGraphForAGivenFramework(projectIdentity, frameworks[0], assetsFile.PackageSpec);
            // TODO https://github.com/nkolev92/DependencyVisualizer/issues/1 - What should we do in the multi framework case?

        }

        private static PackageDependencyGraph GenerateGraphForAGivenFramework(PackageIdentity projectIdentity, LockFileTarget framework, PackageSpec packageSpec)
        {
            PackageDependencyGraph graph = new(new PackageDependencyNode(projectIdentity));

            Dictionary<string, PackageDependencyNode> packageIdToNode = GenerateNodesForAllPackagesInGraph(framework);
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
                string inferedProjectName = Path.GetFileNameWithoutExtension(projectReference.ProjectPath);
                // TODO - https://github.com/nkolev92/DependencyVisualizer/issues/6 What if the package id differs from the project path? We'd miss that.

                PackageDependencyNode node = packageIdToNode[inferedProjectName];
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
                    PackageDependencyNode currentPackageNode = new(new PackageIdentity(package.Name, package.Version));
                    seenPackages.Add(package.Name, currentPackageNode);
                }

                return seenPackages;
            }
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
