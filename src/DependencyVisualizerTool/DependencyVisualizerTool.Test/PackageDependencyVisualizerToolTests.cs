using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Common;
using FluentAssertions;
using FluentAssertions.Equivalency;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Versioning;
using SharedUtility;

namespace DependencyVisualizerTool.Test
{
    public class PackageDependencyVisualizerToolTests
    {
        [Fact]
        public async Task TransGraphToDGMLXDocument_diamonddependency_CreateDGMLCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("DependencyVisualizerTool.Test.compiler.resources.diamonddependency.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.diamonddependency.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public async Task TransGraphToDGMLXDocument_diamonddependencywithouttoplevel_CreateDGMLCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("DependencyVisualizerTool.Test.compiler.resources.diamonddependencywithtoplevel.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.diamonddependencywithtoplevel.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public async Task TransGraphToDGMLXDocument_missingpackageversion_CreateDGMLCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("DependencyVisualizerTool.Test.compiler.resources.missingpackageversion.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.missingpackageversion.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public async Task TransGraphToDGMLXDocument_multipleversions_CreateDGMLCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("DependencyVisualizerTool.Test.compiler.resources.multipleversions.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.multipleversions.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public async Task TransGraphToDGMLXDocument_multitargeted_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.multitargeted.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var dependencyGraphSpec = new DependencyGraphSpec();
            dependencyGraphSpec.AddProject(assetsFile.PackageSpec);
            Dictionary<string, PackageDependencyGraph> graphs = await PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFileAsync(assetsFile, dependencyGraphSpec, projectsOnly: false, new(), CancellationToken.None);
            graphs.Should().HaveCount(2);
            var graph = graphs.First().Value;

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.multitargeted.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public async Task TransGraphToDGMLXDocument_nugetcommon_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.nuget.common.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var dependencyGraphSpec = new DependencyGraphSpec();
            dependencyGraphSpec.AddProject(assetsFile.PackageSpec);
            Dictionary<string, PackageDependencyGraph> graphs = await PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFileAsync(assetsFile, dependencyGraphSpec, projectsOnly: false, new(), CancellationToken.None);
            graphs.Should().HaveCount(2);
            var graph = graphs.First().Value;

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.nuget.common.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }


        [Fact]
        public async Task TransGraphToDGMLXDocument_singlepackagereference_CreateDGMLCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("DependencyVisualizerTool.Test.compiler.resources.singlepackagereference.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.singlepackagereference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML);
        }

        [Fact]
        public async Task TransGraphToDGMLXDocument_singleprojectreference_CreateDGMLCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("DependencyVisualizerTool.Test.compiler.resources.singleprojectreference.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.singleprojectreference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public async Task TransGraphToDGMLXDocument_transitivepackagereference_CreateDGMLCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("DependencyVisualizerTool.Test.compiler.resources.transitivepackagereference.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.transitivepackagereference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public async Task TransGraphToDGMLXDocument_transitiveprojectreference_CreateDGMLCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("DependencyVisualizerTool.Test.compiler.resources.transitiveprojectreference.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.transitiveprojectreference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public async Task TransGraphToDGMLXDocument_WithVulnerableAndDeprecatedPackages_CreateDGMLCorrectly()
        {
            var packageA = new PackageIdentity("A", new NuGetVersion(1, 0, 0));
            var packageB = new PackageIdentity("B", new NuGetVersion(1, 0, 0));
            var packageC = new PackageIdentity("C", new NuGetVersion(1, 1, 0));
            var decorator = new VulnerabilityAndDeprecationDecorator(
                vulnerablePackages: new HashSet<PackageIdentity> { packageA, packageC },
                deprecatedPackages: new HashSet<PackageIdentity> { packageB, packageC });

            var graph = await GetOnlyDependencyGraphAsync("DependencyVisualizerTool.Test.compiler.resources.diamonddependency.assets.json", new() { decorator });

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.diamonddependency.withvulnerabilitiesanddeprecations.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        private async Task<PackageDependencyGraph> GetOnlyDependencyGraphAsync(string resourceName, List<IPackageDependencyNodeDecorator> decorators = null)
        {
            var assetsFileText = TestHelpers.GetResource(resourceName, GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var dependencyGraphSpec = new DependencyGraphSpec();
            dependencyGraphSpec.AddProject(assetsFile.PackageSpec);
            Dictionary<string, PackageDependencyGraph> graphs = await PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFileAsync(assetsFile, dependencyGraphSpec, projectsOnly: false, decorators ?? new(), CancellationToken.None);
            graphs.Should().HaveCount(1);
            var graph = graphs.Single().Value;
            return graph;
        }

        private static string RemoveWhitespace(string s)
        {
            return Regex.Replace(s, @"\s+", string.Empty);
        }

        private class VulnerabilityAndDeprecationDecorator : IPackageDependencyNodeDecorator
        {
            private readonly HashSet<PackageIdentity> _vulnerablePackages;
            private readonly HashSet<PackageIdentity> _deprecatedPackages;

            public VulnerabilityAndDeprecationDecorator(HashSet<PackageIdentity> vulnerablePackages, HashSet<PackageIdentity> deprecatedPackages)
            {
                _vulnerablePackages = vulnerablePackages ?? throw new ArgumentNullException(nameof(vulnerablePackages));
                _deprecatedPackages = deprecatedPackages ?? throw new ArgumentNullException(nameof(deprecatedPackages));
            }

            public Task DecorateAsync(PackageDependencyNode dependencyNode, CancellationToken cancellationToken)
            {
                PackageIdentity identity = dependencyNode.Identity;
                if (_vulnerablePackages.Contains(identity))
                {
                    dependencyNode.Identity.Vulnerable = true;
                }
                if (_deprecatedPackages.Contains(identity))
                {
                    dependencyNode.Identity.Deprecated = true;
                }
                return Task.CompletedTask;
            }
        }
    }
}