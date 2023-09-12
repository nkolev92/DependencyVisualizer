namespace Common
{
    /// <summary>
    /// Represents a DAG and the root node.
    /// </summary>
    /// <typeparam name="T">The type representing the nodes</typeparam>
    /// <typeparam name="EdgeCost">The type representing the edge cost from node to node.</typeparam>
    public class Graph<T, EdgeCost>
    {
        public Node<T, EdgeCost> Node { get; }

        public Graph(Node<T, EdgeCost> node)
        {
            Node = node;
        }
    }
}