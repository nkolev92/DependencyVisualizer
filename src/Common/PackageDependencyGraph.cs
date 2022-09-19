using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace Common
{
    public class PackageDependencyGraph : Graph<PackageIdentity, VersionRange>
    {
        public PackageDependencyGraph(Node<PackageIdentity, VersionRange> node) : base(node)
        {
        }

        public static PackageDependencyGraph FromAssetsFile(LockFile assetsFile)
        {
            if (assetsFile != null)
            {
                PackageIdentity projectName = new(assetsFile.PackageSpec.Name, assetsFile.PackageSpec.Version);
                PackageDependencyGraph graph = new(new PackageDependencyNode(projectName));

                foreach (var framework in assetsFile.Targets.Where(e => string.IsNullOrEmpty(e.RuntimeIdentifier)))
                {
                    Dictionary<string, PackageDependencyNode> seenPackages = GenerateNodesForAllPackagesInGraph(framework);
                    seenPackages.Add(graph.Node.Identity.Id, (PackageDependencyNode)graph.Node); // Make sure we have the current project.

                    foreach (var package in framework.Libraries)
                    {
                        var currentPackageNode = seenPackages[package.Name];

                        foreach (PackageDependency dependency in package.Dependencies)
                        {
                            PackageDependencyNode packageDependencyNode = seenPackages[dependency.Id];

                            currentPackageNode.ChildNodes.Add((packageDependencyNode, dependency.VersionRange));
                            packageDependencyNode.ParentNodes.Add((currentPackageNode, dependency.VersionRange));
                        }
                    }

                    foreach (var package in seenPackages)
                    {
                        if (package.Key != graph.Node.Identity.Id)
                        {
                            if (package.Value.ParentNodes.Count == 0)
                            {
                                graph.Node.ChildNodes.Add((package.Value, new VersionRange(package.Value.Identity.Version))); // Add the correct version range.
                            }
                        }
                    }
                    break; // Be done after one walk.
                    // Add a property to indicate the `Type`, ie. Project vs Package
                }
                return graph;
            }

            throw new InvalidOperationException("Could not parse assets file. Cannot generate the graph.");
        }

        private static LockFile GetAssetsFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            LockFile assetsFile = new LockFileFormat().Read(fileName); // TODO - Pass a logger.
            return assetsFile;
        }

        private static Dictionary<string, PackageDependencyNode> GenerateNodesForAllPackagesInGraph(LockFileTarget framework)
        {
            var seenPackages = new Dictionary<string, PackageDependencyNode>();

            foreach (LockFileTargetLibrary package in framework.Libraries)
            {
                PackageDependencyNode currentPackageNode = new(new PackageIdentity(package.Name, package.Version));
                seenPackages.Add(package.Name, currentPackageNode);
            }

            return seenPackages;
        }
    }
}
