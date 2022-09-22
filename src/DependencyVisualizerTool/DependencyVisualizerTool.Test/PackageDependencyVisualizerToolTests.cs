using System.Text.RegularExpressions;
using Common;
using FluentAssertions;
using NuGet.ProjectModel;
using SharedUtility;

namespace DependencyVisualizerTool.Test
{
    public class PackageDependencyVisualizerToolTests
    {
        [Fact]
        public void TransGraphToDGMLXDocument_diamonddependency_CreateDGMLCorrectly()
        {
            var graph = GetOnlyDependencyGraph("DependencyVisualizerTool.Test.compiler.resources.diamonddependency.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.diamonddependency.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_diamonddependencywithouttoplevel_CreateDGMLCorrectly()
        {
            var graph = GetOnlyDependencyGraph("DependencyVisualizerTool.Test.compiler.resources.diamonddependencywithtoplevel.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.diamonddependencywithtoplevel.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_missingpackageversion_CreateDGMLCorrectly()
        {
            var graph = GetOnlyDependencyGraph("DependencyVisualizerTool.Test.compiler.resources.missingpackageversion.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.missingpackageversion.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_multipleversions_CreateDGMLCorrectly()
        {
            var graph = GetOnlyDependencyGraph("DependencyVisualizerTool.Test.compiler.resources.multipleversions.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.multipleversions.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_multitargeted_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.multitargeted.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graphs = PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFile(assetsFile);
            graphs.Should().HaveCount(2);
            var graph = graphs.First().Value;

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.multitargeted.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_nugetcommon_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.nuget.common.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graphs = PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFile(assetsFile);
            graphs.Should().HaveCount(2);
            var graph = graphs.First().Value;

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.nuget.common.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }


        [Fact]
        public void TransGraphToDGMLXDocument_singlepackagereference_CreateDGMLCorrectly()
        {
            var graph = GetOnlyDependencyGraph("DependencyVisualizerTool.Test.compiler.resources.singlepackagereference.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.singlepackagereference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_singleprojectreference_CreateDGMLCorrectly()
        {
            var graph = GetOnlyDependencyGraph("DependencyVisualizerTool.Test.compiler.resources.singleprojectreference.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.singleprojectreference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_transitivepackagereference_CreateDGMLCorrectly()
        {
            var graph = GetOnlyDependencyGraph("DependencyVisualizerTool.Test.compiler.resources.transitivepackagereference.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.transitivepackagereference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_transitiveprojectreference_CreateDGMLCorrectly()
        {
            var graph = GetOnlyDependencyGraph("DependencyVisualizerTool.Test.compiler.resources.transitiveprojectreference.assets.json");

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.transitiveprojectreference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        private static string RemoveWhitespace(string s)
        {
            return Regex.Replace(s, @"\s+", string.Empty);
        }

        private PackageDependencyGraph GetOnlyDependencyGraph(string resourceName)
        {
            var assetsFileText = TestHelpers.GetResource(resourceName, GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graphs = PackageDependencyGraph.GenerateAllDependencyGraphsFromAssetsFile(assetsFile);
            graphs.Should().HaveCount(1);
            var graph = graphs.Single().Value;
            return graph;
        }
    }
}