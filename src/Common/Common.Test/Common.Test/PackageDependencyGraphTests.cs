using FluentAssertions;
using NuGet.ProjectModel;

namespace Common.Test
{
    public class GraphBuilderTests
    {
        [Fact]
        public void FromAssetsFile_GeneratesGraphWithCorrectRootNode()
        {
            var assetsFileText = TestHelpers.GetResource("Common.Test.compiler.resources.nuget.common.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            graph.Node.Identity.Id.Should().Be("NuGet.Common");
        }
    }
}