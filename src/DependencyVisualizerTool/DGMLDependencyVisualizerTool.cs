﻿using System.Xml.Linq;
using Common;
using NuGet.Versioning;

namespace DependencyVisualizerTool
{
    public class DGMLDependencyVisualizerTool
    {
        private const string DGMLxmlns = "http://schemas.microsoft.com/vs/2009/dgml";

        public static void TransGraphToDGMLFile(PackageDependencyGraph graph, string saveFilePath, bool populateCosts)
        {
            XDocument document = TransGraphToDGMLXDocument(graph, populateCosts);
            document.Save(saveFilePath);
        }

        public static XDocument TransGraphToDGMLXDocument(PackageDependencyGraph graph, bool populateCosts = true)
        {
            // Visited nodes
            Dictionary<string, DGMLNode> nodes = new();
            List<DGMLLink> links = new();

            //BFS on the graph
            Queue<Node<DependencyNodeIdentity, VersionRange>> queue = new();
            Node<DependencyNodeIdentity, VersionRange> firstNode = graph.Node;
            queue.Enqueue(firstNode);
            DGMLNode firstNodeDGML = new DGMLNode(
                                     id: firstNode.Identity.ToString(),
                                     label: firstNode.Identity.ToString(),
                                     type: firstNode.Identity.Type,
                                     isVulnerable: firstNode.Identity.Vulnerable,
                                     isDeprecated: firstNode.Identity.Deprecated);
            nodes.Add(firstNode.Identity.ToString(), firstNodeDGML);

            while (queue.Count > 0)
            {
                Node<DependencyNodeIdentity, VersionRange> current = queue.Dequeue();

                foreach (var child in current.ChildNodes)
                {
                    DGMLLink currentLink = new DGMLLink(
                        source: current.Identity.ToString(),
                        target: child.Item1.Identity.ToString(),
                        label: populateCosts ? child.Item2.ToString() : string.Empty);
                    links.Add(currentLink);
                    if (!nodes.TryGetValue(child.Item1.Identity.ToString(), out _))
                    {
                        queue.Enqueue(child.Item1);
                        DGMLNode currentDGML = new DGMLNode(
                                               id: child.Item1.Identity.ToString(),
                                               label: child.Item1.Identity.ToString(),
                                               type: child.Item1.Identity.Type,
                                               child.Item1.Identity.Vulnerable,
                                               child.Item1.Identity.Deprecated);
                        nodes.Add(child.Item1.Identity.ToString(), currentDGML);
                    }
                }
            }
            XDocument DGMLXDocumenth = GenerateDGMLXDocument(nodes, links);
            return DGMLXDocumenth;
        }

        private static XDocument GenerateDGMLXDocument(Dictionary<string, DGMLNode> nodes, List<DGMLLink> links)
        {
            var document = new XDocument(
                new XElement(XName.Get("DirectedGraph", DGMLxmlns),
                //Add Nodes
                new XElement(XName.Get("Nodes", DGMLxmlns),
                    from item in nodes
                    select new XElement(XName.Get("Node", DGMLxmlns),
                                        new XAttribute("Id", item.Value.Id),
                                        new XAttribute("Label", item.Value.Label),
                                        new XAttribute("Category", item.Value.Category))),
                //Add Links
                new XElement(XName.Get("Links", DGMLxmlns),
                    from item in links
                    select new XElement(XName.Get("Link", DGMLxmlns),
                                       new XAttribute("Source", item.Source),
                                       new XAttribute("Target", item.Target),
                                       new XAttribute("Label", item.Label))),
                //Add Categories
                new XElement(XName.Get("Categories", DGMLxmlns),
                new XElement(XName.Get("Category", DGMLxmlns),
                             new XAttribute("Id", "Project"),
                             new XAttribute("Background", "Lightblue"),
                             new XAttribute("StrokeThickness", "2")),
                new XElement(XName.Get("Category", DGMLxmlns),
                            new XAttribute("Id", "Package"),
                            new XAttribute("Background", "None"),
                            new XAttribute("StrokeThickness", "1")),
                new XElement(XName.Get("Category", DGMLxmlns),
                            new XAttribute("Id", "VulnerablePackage"),
                            new XAttribute("Background", "None"),
                            new XAttribute("StrokeThickness", "4"),
                            new XAttribute("Stroke", "Red")),
                new XElement(XName.Get("Category", DGMLxmlns),
                            new XAttribute("Id", "DeprecatedPackage"),
                            new XAttribute("Background", "Yellow"),
                            new XAttribute("StrokeThickness", "1")),
                new XElement(XName.Get("Category", DGMLxmlns),
                            new XAttribute("Id", "VulnerableAndDeprecatedPackage"),
                            new XAttribute("Background", "Yellow"),
                            new XAttribute("StrokeThickness", "4"),
                            new XAttribute("Stroke", "Red"))))
            );
            return document;
        }

        private class DGMLNode : IEquatable<DGMLNode>
        {
            public string Id { get; set; }

            public string Label { get; set; }

            public string Category { get; set; }

            public DGMLNode(string id, string label, DependencyType type, bool isVulnerable, bool isDeprecated)
            {
                this.Id = id;
                this.Label = label;
                this.Category = GetCategory(type, isVulnerable, isDeprecated);
            }

            private static string GetCategory(DependencyType type, bool isVulnerable, bool isDeprecated)
            {
                return isVulnerable && isDeprecated ?
                    "VulnerableAndDeprecatedPackage" :
                    isVulnerable ?
                        "VulnerablePackage" :
                        isDeprecated ?
                            "DeprecatedPackage" :
                            type.ToString();
            }

            public bool Equals(DGMLNode? other)
            {
                if (other != null)
                {
                    return Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }
        }

        private class DGMLLink
        {
            public string Source { get; set; }

            public string Target { get; set; }

            public string Label { get; set; }

            public DGMLLink(string source, string target, string label)
            {
                this.Source = source;
                this.Target = target;
                this.Label = label;
            }
        }
    }
}