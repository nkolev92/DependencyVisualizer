using FluentAssertions;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace Common.Test
{
    // TODO NK - Add descriptions in the tests.
    public class GraphBuilderTests
    {
        [Fact]
        public void FromAssetsFile_WithLargeGraph_ParsesCorrectRootNodeGraph()
        {
            var assetsFileText = TestHelpers.GetResource("Common.Test.compiler.resources.nuget.common.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            graph.Node.Identity.Id.Should().Be("NuGet.Common");
        }

        [Fact]
        public void FromAssetsFile_WithSingleFramework_WithSinglePackageReference_ParsesGraphCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("Common.Test.compiler.resources.singlepackagereference.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            graph.Node.Identity.Id.Should().Be("SingleProjectSingleFramework");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(1);
            (Node<PackageIdentity, VersionRange>, VersionRange) newtonsoftJsonNode = graph.Node.ChildNodes[0];
            newtonsoftJsonNode.Item1.Identity.Should().Be(new PackageIdentity("Newtonsoft.json", new NuGetVersion(13, 0, 1)));
            newtonsoftJsonNode.Item1.ParentNodes.Should().HaveCount(1);
            newtonsoftJsonNode.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalNode(graph.Node, newtonsoftJsonNode, VersionRange.Parse("13.0.1"));
        }

        private static void ValidateBidirectionalNode(Node<PackageIdentity, VersionRange> parentNode, (Node<PackageIdentity, VersionRange>, VersionRange) childNode, VersionRange expectedVersionRange)
        {
            childNode.Item1.ParentNodes.Should().Contain(e => e.Item1.Equals(parentNode)); // Ensure child's parent node contains parent
            parentNode.ChildNodes.Should().Contain(e => e.Item1.Equals(childNode.Item1)); // Ensure parent's child node contains parent (redudant check)
            childNode.Item2.Should().Be(expectedVersionRange); // Ensure the child version range
            var parentToChildEdge = parentNode.ChildNodes.First(e => e.Item1.Equals(childNode.Item1)); 
            parentToChildEdge.Item2.Should().Be(expectedVersionRange); // Ensure the parent to child version range
        }

        [Fact]
        public void FromAssetsFile_WithSingleFramework_WithSingleProjectReference_ParsesGraphCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("Common.Test.compiler.resources.singleprojectreference.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            graph.Node.Identity.Id.Should().Be("Parent");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(1);
            
            // Ensure Parent => Leaf
            (Node<PackageIdentity, VersionRange>, VersionRange) leafNode = graph.Node.ChildNodes[0];
            leafNode.Item1.Identity.Should().Be(new PackageIdentity("Leaf", new NuGetVersion(3, 0, 0)));
            leafNode.Item1.ParentNodes.Should().HaveCount(1);
            leafNode.Item1.ChildNodes.Should().HaveCount(0);
            ValidateBidirectionalNode(graph.Node, leafNode, VersionRange.Parse("3.0.0"));
        }

        [Fact]
        public void FromAssetsFile_WithTransitivePackageReference_ParsesGraphCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("Common.Test.compiler.resources.transitivepackagereference.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            graph.Node.Identity.Id.Should().Be("TransitivePackageReference");
            graph.Node.ParentNodes.Should().HaveCount(0);
            graph.Node.ChildNodes.Should().HaveCount(1);

            // Ensure TransitivePackageReference => NuGet.Common
            (Node<PackageIdentity, VersionRange>, VersionRange) directPackageReference = graph.Node.ChildNodes[0];
            directPackageReference.Item1.Identity.Should().Be(new PackageIdentity("NuGet.Common", new NuGetVersion(6, 3, 0)));
            directPackageReference.Item1.ParentNodes.Should().HaveCount(1);
            directPackageReference.Item1.ChildNodes.Should().HaveCount(1);
            ValidateBidirectionalNode(graph.Node, directPackageReference, VersionRange.Parse("6.3.0"));
        }

        [Fact]
        public void FromAssetsFile_WithTransitiveProjectReference_ParsesGraphCorrectly()
        {
        }

        [Fact]
        public void FromAssetsFile_WithPackageReference_AndMinVersionDoesNotMatchSelectedVersion_ParsesGraphCorrectly()
        {
        }

        [Fact]
        public void FromAssetsFile_WithMultipleDirectAndTransitivePackageReferences_ParsesGraphCorrectly()
        {
        }

        [Fact]
        public void FromAssetsFile_WithADiamondDependency_ParsesGraphCorrectly()
        {
        }

        [Fact]
        public void FromAssetsFile_WithADiamondDependencyThroughAProject_ParsesGraphCorrectly()
        {
        }

        [Fact]
        public void FromAssetsFile_WithAMultiLevelDiamondDependency_ParsesGraphCorrectly()
        {
        }

        [Fact]
        public void FromAssetsFile_WithADiamondDependencyAsDirectReference_ParsesGraphCorrectly()
        {
        }

        [Fact]
        public void FromAssetsFile_WithMultipleFrameworksAndDifferentPackageReferences_ParsesOnlyTheFirstGraphCorrectly()
        {
        }
    }
}