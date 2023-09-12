using FluentAssertions;
using NuGet.ProjectModel;
using NuGet.Versioning;
using SharedUtility;

namespace Common.Test
{
    public class GraphBuilderTests
    {
        [Fact]
        public async Task FromAssetsFile_WithLargeGraph_ParsesGraphCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("Common.Test.compiler.resources.nuget.common.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var dependencyGraphSpec = new DependencyGraphSpec();
            dependencyGraphSpec.AddProject(assetsFile.PackageSpec);
            var graphs = await PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFileAsync(assetsFile, dependencyGraphSpec, new GraphOptions(generateProjectsOnly: false), new(), CancellationToken.None);
            graphs.Should().HaveCount(2);

            var graph = graphs.First().Value;
            graph.Node.Identity.Id.Should().Be("NuGet.Common");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(5);

            // Ensure NuGet.Common => Microsoft.CodeAnalysis.BannedApiAnalyzers
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) bannedApiAnalyzers = graph.Node.ChildNodes[0];
            bannedApiAnalyzers.Item1.Identity.Should().Be(new DependencyNodeIdentity("Microsoft.CodeAnalysis.BannedApiAnalyzers", new NuGetVersion(3, 3, 2), DependencyType.Package));
            bannedApiAnalyzers.Item1.ParentNodes.Should().HaveCount(1);
            bannedApiAnalyzers.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(graph.Node, bannedApiAnalyzers, VersionRange.Parse("3.3.2"));

            // Ensure NuGet.Common => Microsoft.CodeAnalysis.FxCopAnalyzers
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) fxCopAnalyzers = graph.Node.ChildNodes[1];
            fxCopAnalyzers.Item1.Identity.Should().Be(new DependencyNodeIdentity("Microsoft.CodeAnalysis.FxCopAnalyzers", new NuGetVersion(2, 9, 8), DependencyType.Package));
            fxCopAnalyzers.Item1.ParentNodes.Should().HaveCount(1);
            fxCopAnalyzers.Item1.ChildNodes.Should().HaveCount(4);
            ValidateBidirectionalEdges(graph.Node, fxCopAnalyzers, VersionRange.Parse("2.9.8"));

            // Ensure NuGet.Common => Microsoft.CodeAnalysis.PublicApiAnalyzers
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) publicApiAnalyzers = graph.Node.ChildNodes[2];
            publicApiAnalyzers.Item1.Identity.Should().Be(new DependencyNodeIdentity("Microsoft.CodeAnalysis.PublicApiAnalyzers", new NuGetVersion(3, 0, 0), DependencyType.Package));
            publicApiAnalyzers.Item1.ParentNodes.Should().HaveCount(1);
            publicApiAnalyzers.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(graph.Node, publicApiAnalyzers, VersionRange.Parse("3.0.0"));

            // Ensure NuGet.Common => Microsoft.SourceLink.GitHub
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) sourcelink = graph.Node.ChildNodes[3];
            sourcelink.Item1.Identity.Should().Be(new DependencyNodeIdentity("Microsoft.SourceLink.GitHub", new NuGetVersion(1, 0, 0), DependencyType.Package));
            sourcelink.Item1.ParentNodes.Should().HaveCount(1);
            sourcelink.Item1.ChildNodes.Should().HaveCount(2);
            ValidateBidirectionalEdges(graph.Node, sourcelink, VersionRange.Parse("1.0.0"));

            // Ensure NuGet.Common => NuGet.Frameworks
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) nugetFrameworks = graph.Node.ChildNodes[4];
            nugetFrameworks.Item1.Identity.Should().Be(new DependencyNodeIdentity("NuGet.Frameworks", new NuGetVersion(6, 4, 0, "preview.3.32767"), DependencyType.Project));
            nugetFrameworks.Item1.ParentNodes.Should().HaveCount(1);
            nugetFrameworks.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(graph.Node, nugetFrameworks, VersionRange.Parse("6.4.0-preview.3.32767"));
        }

        // SingleProjectSingleFramework -> Newtonsoft.Json 13.0.1
        [Fact]
        public async Task FromAssetsFile_WithSingleFramework_WithSinglePackageReference_ParsesGraphCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("Common.Test.compiler.resources.singlepackagereference.assets.json");

            graph.Node.Identity.Id.Should().Be("SingleProjectSingleFramework");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(1);
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) newtonsoftJsonNode = graph.Node.ChildNodes[0];
            newtonsoftJsonNode.Item1.Identity.Should().Be(new DependencyNodeIdentity("Newtonsoft.json", new NuGetVersion(13, 0, 1), DependencyType.Package));
            newtonsoftJsonNode.Item1.ParentNodes.Should().HaveCount(1);
            newtonsoftJsonNode.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(graph.Node, newtonsoftJsonNode, VersionRange.Parse("13.0.1"));
        }

        // Parent -> Leaf 3.0.0
        [Fact]
        public async Task FromAssetsFile_WithSingleFramework_WithSingleProjectReference_ParsesGraphCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("Common.Test.compiler.resources.singleprojectreference.assets.json");

            graph.Node.Identity.Id.Should().Be("Parent");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(1);

            // Ensure Parent => Leaf
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) leafNode = graph.Node.ChildNodes[0];
            leafNode.Item1.Identity.Should().Be(new DependencyNodeIdentity("Leaf", new NuGetVersion(3, 0, 0), DependencyType.Project));
            leafNode.Item1.ParentNodes.Should().HaveCount(1);
            leafNode.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(graph.Node, leafNode, VersionRange.Parse("3.0.0"));
        }

        // TransitivePackageReference -> NuGet.Common 6.3.0 -> NuGet.Frameworks 6.3.0
        [Fact]
        public async Task FromAssetsFile_WithTransitivePackageReference_ParsesGraphCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("Common.Test.compiler.resources.transitivepackagereference.assets.json");

            graph.Node.Identity.Id.Should().Be("TransitivePackageReference");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(1);

            // Ensure TransitivePackageReference => NuGet.Common
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) directPackageReference = graph.Node.ChildNodes[0];
            directPackageReference.Item1.Identity.Should().Be(new DependencyNodeIdentity("NuGet.Common", new NuGetVersion(6, 3, 0), DependencyType.Package));
            directPackageReference.Item1.ParentNodes.Should().HaveCount(1);
            directPackageReference.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalEdges(graph.Node, directPackageReference, VersionRange.Parse("6.3.0"));

            // Ensure NuGet.Common => NuGet.Versioning
            var transitivePackageReference = directPackageReference.Item1.ChildNodes[0];
            transitivePackageReference.Item1.Identity.Should().Be(new DependencyNodeIdentity("NuGet.Frameworks", new NuGetVersion(6, 3, 0), DependencyType.Package));
            transitivePackageReference.Item1.ParentNodes.Should().HaveCount(1);
            transitivePackageReference.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(directPackageReference.Item1, transitivePackageReference, VersionRange.Parse("6.3.0"));


        }

        // ParentProject -> LeafProject 1.0.0 -> NuGet.Versioning 6.3.0
        [Fact]
        public async Task FromAssetsFile_WithTransitiveProjectReference_ParsesGraphCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("Common.Test.compiler.resources.transitiveprojectreference.assets.json");

            graph.Node.Identity.Id.Should().Be("ParentProject");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(1);

            // Ensure ParentProject => LeafProject
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) directPackageReference = graph.Node.ChildNodes[0];
            directPackageReference.Item1.Identity.Should().Be(new DependencyNodeIdentity("LeafProject", new NuGetVersion(1, 0, 0), DependencyType.Project));
            directPackageReference.Item1.ParentNodes.Should().HaveCount(1);
            directPackageReference.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalEdges(graph.Node, directPackageReference, VersionRange.Parse("1.0.0"));

            // Ensure NuGet.Common => NuGet.Versioning
            var transitivePackageReference = directPackageReference.Item1.ChildNodes[0];
            transitivePackageReference.Item1.Identity.Should().Be(new DependencyNodeIdentity("NuGet.Versioning", new NuGetVersion(6, 3, 0), DependencyType.Package));
            transitivePackageReference.Item1.ParentNodes.Should().HaveCount(1);
            transitivePackageReference.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(directPackageReference.Item1, transitivePackageReference, VersionRange.Parse("6.3.0"));

        }

        // Project -> (>= 6.2.5) NuGet.Common 6.3.0 -> NuGet.Frameworks 6.3.0
        [Fact]
        public async Task FromAssetsFile_WithPackageReference_AndMinVersionDoesNotMatchSelectedVersion_ParsesGraphCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("Common.Test.compiler.resources.missingpackageversion.assets.json");

            graph.Node.Identity.Id.Should().Be("Project");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(1);

            // Ensure Project => NuGet.Common
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) directPackageReference = graph.Node.ChildNodes[0];
            directPackageReference.Item1.Identity.Should().Be(new DependencyNodeIdentity("NuGet.Common", new NuGetVersion(6, 3, 0), DependencyType.Package));
            directPackageReference.Item1.ParentNodes.Should().HaveCount(1);
            directPackageReference.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalEdges(graph.Node, directPackageReference, VersionRange.Parse("6.2.5"));

            // Ensure NuGet.Common => NuGet.Frameworks
            var transitivePackageReference = directPackageReference.Item1.ChildNodes[0];
            transitivePackageReference.Item1.Identity.Should().Be(new DependencyNodeIdentity("NuGet.Frameworks", new NuGetVersion(6, 3, 0), DependencyType.Package));
            transitivePackageReference.Item1.ParentNodes.Should().HaveCount(1);
            transitivePackageReference.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(directPackageReference.Item1, transitivePackageReference, VersionRange.Parse("6.3.0"));
        }

        // Project -> NuGet.Common 6.3.0 -> NuGet.Frameworks 6.3.0
        // Project -> Newtonsoft.Json.Bson 1.0.2 -> Newtonsoft.Json 12.0.1
        [Fact]
        public async Task FromAssetsFile_WithMultipleDirectAndTransitivePackageReferences_ParsesGraphCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("Common.Test.compiler.resources.multipleversions.assets.json");

            graph.Node.Identity.Id.Should().Be("Project");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(2);

            // Ensure Project => Newtonsoft.Json.Bson
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) newtonsoftjsonbson = graph.Node.ChildNodes[0];
            newtonsoftjsonbson.Item1.Identity.Should().Be(new DependencyNodeIdentity("Newtonsoft.Json.Bson", new NuGetVersion(1, 0, 2), DependencyType.Package));
            newtonsoftjsonbson.Item1.ParentNodes.Should().HaveCount(1);
            newtonsoftjsonbson.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalEdges(graph.Node, newtonsoftjsonbson, VersionRange.Parse("1.0.2"));

            // Ensure Project => NuGet.Common
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) nugetcommon = graph.Node.ChildNodes[1];
            nugetcommon.Item1.Identity.Should().Be(new DependencyNodeIdentity("NuGet.Common", new NuGetVersion(6, 3, 0), DependencyType.Package));
            nugetcommon.Item1.ParentNodes.Should().HaveCount(1);
            nugetcommon.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalEdges(graph.Node, nugetcommon, VersionRange.Parse("6.3.0"));

            // Ensure NuGet.Common => NuGet.Versioning
            var nugetversioning = nugetcommon.Item1.ChildNodes[0];
            nugetversioning.Item1.Identity.Should().Be(new DependencyNodeIdentity("NuGet.Frameworks", new NuGetVersion(6, 3, 0), DependencyType.Package));
            nugetversioning.Item1.ParentNodes.Should().HaveCount(1);
            nugetversioning.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(nugetcommon.Item1, nugetversioning, VersionRange.Parse("6.3.0"));

            // Ensure Newtonsoft.Json.Bson => Newtonsoft.Json
            var newtonsoftjson = newtonsoftjsonbson.Item1.ChildNodes[0];
            newtonsoftjson.Item1.Identity.Should().Be(new DependencyNodeIdentity("Newtonsoft.Json", new NuGetVersion(12, 0, 1), DependencyType.Package));
            newtonsoftjson.Item1.ParentNodes.Should().HaveCount(1);
            newtonsoftjson.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(newtonsoftjsonbson.Item1, newtonsoftjson, VersionRange.Parse("12.0.1"));
        }

        // TestProject -> A 1.0.0 -> (>= 1.0.0) C 1.1.0
        // TestProject -> B 1.0.0 -> (>= 1.1.0) C 1.1.0
        [Fact]
        public async Task FromAssetsFile_WithADiamondDependency_ParsesGraphCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("Common.Test.compiler.resources.diamonddependency.assets.json");

            graph.Node.Identity.Id.Should().Be("TestProject");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(2);

            // Ensure TestProject => A
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) a = graph.Node.ChildNodes[0];
            a.Item1.Identity.Should().Be(new DependencyNodeIdentity("A", new NuGetVersion(1, 0, 0), DependencyType.Package));
            a.Item1.ParentNodes.Should().HaveCount(1);
            a.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalEdges(graph.Node, a, VersionRange.Parse("1.0.0"));

            // Ensure TestProject => B
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) b = graph.Node.ChildNodes[1];
            b.Item1.Identity.Should().Be(new DependencyNodeIdentity("b", new NuGetVersion(1, 0, 0), DependencyType.Package));
            b.Item1.ParentNodes.Should().HaveCount(1);
            b.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalEdges(graph.Node, b, VersionRange.Parse("1.0.0"));

            // Ensure A => C
            var c1 = a.Item1.ChildNodes[0];
            c1.Item1.Identity.Should().Be(new DependencyNodeIdentity("c", new NuGetVersion(1, 1, 0), DependencyType.Package));
            c1.Item1.ParentNodes.Should().HaveCount(2);
            c1.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(a.Item1, c1, VersionRange.Parse("1.0.0"));

            // Ensure B => C
            var c2 = b.Item1.ChildNodes[0];
            c2.Item1.Identity.Should().Be(new DependencyNodeIdentity("c", new NuGetVersion(1, 1, 0), DependencyType.Package));
            c2.Item1.ParentNodes.Should().HaveCount(2);
            c2.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(b.Item1, c2, VersionRange.Parse("1.1.0"));

            // Ensure C1 and C2 are the same node
            c1.Item1.Should().Be(c2.Item1);
        }

        // TestProject -> A 1.0.0 -> (>= 1.0.0) B 2.0.0
        // TestProject -> B 2.0.0 
        [Fact]
        public async Task FromAssetsFile_WithADiamondDependencyAsDirectReference_ParsesGraphCorrectly()
        {
            var graph = await GetOnlyDependencyGraphAsync("Common.Test.compiler.resources.diamonddependencywithtoplevel.assets.json");

            graph.Node.Identity.Id.Should().Be("TestProject");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(2);

            // Ensure TestProject => A
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) a = graph.Node.ChildNodes[0];
            a.Item1.Identity.Should().Be(new DependencyNodeIdentity("A", new NuGetVersion(1, 0, 0), DependencyType.Package));
            a.Item1.ParentNodes.Should().HaveCount(1);
            a.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalEdges(graph.Node, a, VersionRange.Parse("1.0.0"));

            // Ensure TestProject => B
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) b1 = graph.Node.ChildNodes[1];
            b1.Item1.Identity.Should().Be(new DependencyNodeIdentity("B", new NuGetVersion(2, 0, 0), DependencyType.Package));
            b1.Item1.ParentNodes.Should().HaveCount(2);
            b1.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(graph.Node, b1, VersionRange.Parse("2.0.0"));

            // Ensure A => B
            var b2 = a.Item1.ChildNodes[0];
            b2.Item1.Identity.Should().Be(new DependencyNodeIdentity("B", new NuGetVersion(2, 0, 0), DependencyType.Package));
            b2.Item1.ParentNodes.Should().HaveCount(2);
            b2.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(a.Item1, b2, VersionRange.Parse("1.0.0"));

            // Ensure B1 and B2 are the same node
            b2.Item1.Should().Be(b2.Item1);
        }

        // (net472) TestProject -> A 1.0.0
        // (net48) TestProject -> B 2.0.0
        [Fact]
        public async Task GenerateAllDependencyGraphsFromAssetsFile_WithMultipleFrameworksAndDifferentPackageReferences_ParsesOnlyTheFirstGraphCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("Common.Test.compiler.resources.multitargeted.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var dependencyGraphSpec = new DependencyGraphSpec();
            dependencyGraphSpec.AddProject(assetsFile.PackageSpec);
            Dictionary<string, PackageDependencyGraph> graphs = await PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFileAsync(assetsFile, dependencyGraphSpec, new GraphOptions(generateProjectsOnly: false), new(), CancellationToken.None);

            var net472Graph = graphs["net472"];
            net472Graph.Node.Identity.Id.Should().Be("TestProject");
            net472Graph.Node.ParentNodes.Should().HaveCount(0);
            net472Graph.Node.ChildNodes.Should().HaveCount(1);
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) a = net472Graph.Node.ChildNodes[0];
            a.Item1.Identity.Should().Be(new DependencyNodeIdentity("A", new NuGetVersion(1, 0, 0), DependencyType.Package));
            a.Item1.ParentNodes.Should().HaveCount(1);
            a.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(net472Graph.Node, a, VersionRange.Parse("1.0.0"));

            var net48Graph = graphs["net48"];
            net48Graph.Node.Identity.Id.Should().Be("TestProject");
            net48Graph.Node.ParentNodes.Should().HaveCount(0);
            net48Graph.Node.ChildNodes.Should().HaveCount(1);
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) b = net48Graph.Node.ChildNodes[0];
            b.Item1.Identity.Should().Be(new DependencyNodeIdentity("b", new NuGetVersion(2, 0, 0), DependencyType.Package));
            b.Item1.ParentNodes.Should().HaveCount(1);
            b.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(net48Graph.Node, b, VersionRange.Parse("2.0.0"));
        }

        // Parent -> Nephew (project file name is Child.csproj) 13.0.1 => NuGet.Frameworks 6.3.0
        [Fact]
        public async Task FromAssetsFile_WithProjectReferenceWithAPackageId_ParsesGraphCorrectly()
        {
            using var tempFile = new TempFile();
            var assetsFileText = TestHelpers.GetResource("Common.Test.compiler.resources.projectwithpackageid.assets.json", GetType());
            var dgspecFileText = TestHelpers.GetResource("Common.Test.compiler.resources.projectwithpackageid.dgspec.json", GetType());
            File.WriteAllText(tempFile.FilePath, dgspecFileText);
            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var dgSpec = DependencyGraphSpec.Load(tempFile.FilePath);

            Dictionary<string, PackageDependencyGraph> graphs = await PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFileAsync(assetsFile, dgSpec, new GraphOptions(generateProjectsOnly: false), new(), CancellationToken.None);
            graphs.Should().HaveCount(1);
            var graph = graphs.Single().Value;

            graph.Node.Identity.Id.Should().Be("Parent");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(1);

            // Ensure Parent => Child (but really named Nephew)
            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) nephewNode = graph.Node.ChildNodes[0];
            nephewNode.Item1.Identity.Should().Be(new DependencyNodeIdentity("Nephew", new NuGetVersion(1, 0, 0), DependencyType.Project));
            nephewNode.Item1.ParentNodes.Should().HaveCount(1);
            nephewNode.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalEdges(graph.Node, nephewNode, VersionRange.Parse("1.0.0"));

            (Node<DependencyNodeIdentity, VersionRange>, VersionRange) nugetVersioning = nephewNode.Item1.ChildNodes[0];
            nugetVersioning.Item1.Identity.Should().Be(new DependencyNodeIdentity("NuGet.Frameworks", new NuGetVersion(6, 3, 0), DependencyType.Package));
            nugetVersioning.Item1.ParentNodes.Should().HaveCount(1);
            nugetVersioning.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalEdges(nephewNode.Item1, nugetVersioning, VersionRange.Parse("6.3.0"));
        }

        private async Task<PackageDependencyGraph> GetOnlyDependencyGraphAsync(string resourceName)
        {
            var assetsFileText = TestHelpers.GetResource(resourceName, GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var dependencyGraphSpec = new DependencyGraphSpec();
            dependencyGraphSpec.AddProject(assetsFile.PackageSpec);
            Dictionary<string, PackageDependencyGraph> graphs = await PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFileAsync(assetsFile, dependencyGraphSpec, new GraphOptions(generateProjectsOnly: false), new(), CancellationToken.None);
            graphs.Should().HaveCount(1);
            var graph = graphs.Single().Value;
            return graph;
        }

        private static void ValidateBidirectionalEdges(Node<DependencyNodeIdentity, VersionRange> parentNode, (Node<DependencyNodeIdentity, VersionRange>, VersionRange) childNode, VersionRange expectedVersionRange)
        {
            childNode.Item1.ParentNodes.Should().Contain(e => e.Item1.Equals(parentNode)); // Ensure child's parent node contains parent
            parentNode.ChildNodes.Should().Contain(e => e.Item1.Equals(childNode.Item1)); // Ensure parent's child node contains parent (redudant check)
            childNode.Item2.Should().Be(expectedVersionRange); // Ensure the child version range
            var parentToChildEdge = parentNode.ChildNodes.First(e => e.Item1.Equals(childNode.Item1));
            parentToChildEdge.Item2.Should().Be(expectedVersionRange); // Ensure the parent to child version range
        }
    }
}