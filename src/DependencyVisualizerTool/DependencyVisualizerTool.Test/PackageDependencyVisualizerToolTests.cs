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
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.diamonddependency.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.diamonddependency.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_diamonddependencywithouttoplevel_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.diamonddependencywithtoplevel.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.diamonddependencywithtoplevel.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_missingpackageversion_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.missingpackageversion.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.missingpackageversion.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_multipleversions_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.multipleversions.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.multipleversions.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_multitargeted_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.multitargeted.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.multitargeted.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_nugetcommon_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.nuget.common.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.nuget.common.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }


        [Fact]
        public void TransGraphToDGMLXDocument_singlepackagereference_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.singlepackagereference.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.singlepackagereference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_singleprojectreference_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.singleprojectreference.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.singleprojectreference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_transitivepackagereference_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.transitivepackagereference.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.transitivepackagereference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        [Fact]
        public void TransGraphToDGMLXDocument_transitiveprojectreference_CreateDGMLCorrectly()
        {
            var assetsFileText = TestHelpers.GetResource("DependencyVisualizerTool.Test.compiler.resources.transitiveprojectreference.assets.json", GetType());

            var assetsFile = new LockFileFormat().Parse(assetsFileText, Path.GetTempPath());
            var graph = PackageDependencyGraph.FromAssetsFile(assetsFile);

            string actualDGML = RemoveWhitespace(DGMLDependencyVisualizerTool.TransGraphToDGMLXDocument(graph).ToString());

            string expectedDGML = RemoveWhitespace(TestHelpers.GetResource($"DependencyVisualizerTool.Test.compiler.resources.transitiveprojectreference.dgml", GetType()));

            actualDGML.Should().Be(expectedDGML, because: actualDGML);
        }

        private static string RemoveWhitespace(string s)
        {
            return Regex.Replace(s, @"\s+", string.Empty);
        }
    }
}