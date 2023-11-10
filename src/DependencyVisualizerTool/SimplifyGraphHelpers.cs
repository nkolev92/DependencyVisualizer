using Common;
using NuGet.ProjectModel;

namespace DependencyVisualizerTool
{
    public static class SimplifyGraphHelpers
    {
        public static int SimplifyGraph(DependencyGraphSpec dgspecFile, Dictionary<string, PackageDependencyGraph> dictGraph, bool dryRun)
        {
            foreach (var packageDependencyGraph in dictGraph)
            {
                Console.WriteLine($"Analyzing {packageDependencyGraph.Key} for {packageDependencyGraph.Value}");
                var projectsInOrder = DependencyGraphSpec.SortPackagesByDependencyOrder(dgspecFile.Projects);
                Dictionary<string, string> nameToPath = GetProjectNameToProjectPathMap(projectsInOrder);

                int totalReferencesToRemove = 0;
                int totalReferencesRemoved = 0;

                foreach (var project in projectsInOrder)
                {
                    var name = project.Name;
                    var projectPath = project.FilePath;
                    var node = GetProjectNode(name, packageDependencyGraph.Value);
                    if (node == null) // projectsInOrder contains *all* projects, but not every project is a part of every framework.
                    {
                        continue;
                    }
                    var projectNamesToRemove = FindSuggestedProjectReferencesToRemove(node);
                    var toRemove = projectNamesToRemove.Select(e => Path.GetFileName(nameToPath[e])).ToList();
                    totalReferencesToRemove += toRemove.Count;

                    if (toRemove.Any())
                    {
                        if (dryRun)
                        {
                            Console.WriteLine(projectPath + ": Redundant references: " + string.Join(",", toRemove));
                        }
                        else
                        {
                            totalReferencesRemoved += RemoveReferencesFromProject(projectPath, toRemove);
                        }
                    }
                }
                Console.WriteLine($"References to remove: {totalReferencesToRemove}, References removed: {totalReferencesRemoved}");
            }

            return 0;

            static Dictionary<string, string> GetProjectNameToProjectPathMap(IReadOnlyList<PackageSpec> projectsInOrder)
            {
                Dictionary<string, string> nameToPath = new();
                foreach (var project in projectsInOrder)
                {
                    nameToPath.Add(project.Name, project.FilePath);
                }

                return nameToPath;
            }

            static int RemoveReferencesFromProject(string projectPath, List<string> toRemove)
            {
                int totalReferencesRemoved = 0;
                if (toRemove.Any())
                {
                    var lines = File.ReadAllLines(projectPath);
                    var newLines = new List<string>();
                    bool removedAtLeastOne = false;
                    foreach (var line in lines)
                    {
                        if (!ShouldRemoveLine(line, toRemove))
                        {
                            newLines.Add(line);
                        }
                        else
                        {
                            totalReferencesRemoved++;
                            removedAtLeastOne = true;
                        }
                    }
                    if (removedAtLeastOne)
                    {
                        File.WriteAllLines(projectPath, newLines);
                        Console.WriteLine($"Updated {projectPath}");
                    }
                    else
                    {
                        Console.WriteLine($"Did not update {projectPath}");
                    }
                }

                return totalReferencesRemoved;

                static bool ShouldRemoveLine(string line, List<string> projectRefsToRemove)
                {
                    foreach (var projectToRemove in projectRefsToRemove)
                    {
                        if (line.Contains(projectToRemove, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        public static Node<DependencyNodeIdentity, NuGet.Versioning.VersionRange> GetProjectNode(string name, PackageDependencyGraph packageDependencyGraph)
        {
            HashSet<string> visited = new HashSet<string>();

            Stack<Node<DependencyNodeIdentity, NuGet.Versioning.VersionRange>> toProcess = new();
            toProcess.Push(packageDependencyGraph.Node);

            while (toProcess.Any())
            {
                var node = toProcess.Pop();
                if (visited.Contains(node.Identity.Id))
                {
                    continue;
                }

                if (node.Identity.Id == name) return node;

                visited.Add(node.Identity.Id);
                foreach (var child in node.ChildNodes)
                {
                    toProcess.Push(child.Item1);
                }

            }
            return null;
        }

        // Find any projects that are direct reference to the current project, but also appear transitively.
        private static List<string> FindSuggestedProjectReferencesToRemove(Node<DependencyNodeIdentity, NuGet.Versioning.VersionRange> project)
        {
            var directReferences = new List<string>();

            Stack<(Node<DependencyNodeIdentity, NuGet.Versioning.VersionRange>, bool)> toProcess = new();

            foreach (var directReference in project.ChildNodes)
            {
                directReferences.Add(directReference.Item1.Identity.Id);
                toProcess.Push((directReference.Item1, true));
            }

            HashSet<string> visited = new HashSet<string>();
            HashSet<string> transitive = new HashSet<string>();

            while (toProcess.Any())
            {
                var node = toProcess.Pop();
                if (visited.Contains(node.Item1.Identity.Id))
                {
                    continue;
                }
                visited.Add(node.Item1.Identity.Id);

                if (!node.Item2)
                {
                    transitive.Add(node.Item1.Identity.Id);
                }
                foreach (var child in node.Item1.ChildNodes)
                {
                    toProcess.Push((child.Item1, false));
                }

            }

            return transitive.Intersect(directReferences).ToList();
        }
    }
}