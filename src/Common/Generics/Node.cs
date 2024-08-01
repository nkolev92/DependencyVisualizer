namespace Common;

/// <summary>
/// Represents a node, or a vertex in the DAG.
/// </summary>
/// <typeparam name="T">The type describing the identity of the node.</typeparam>
/// <typeparam name="EdgeCost">The type denoting the cost for each edge from this node to any other node.</typeparam>
public class Node<T, EdgeCost>
{
    /// <summary>
    /// Identity type. It is recommend that all the node information is included in this type.
    /// </summary>
    public T Identity { get; }

    /// <summary>
    /// A never null list of parent nodes and their edge costs. The order is not relevant. Note that this is a DAG, but the ParentNodes are still contain for convenience.
    /// </summary>
    public IList<(Node<T, EdgeCost>, EdgeCost)> ParentNodes { get; } = new List<(Node<T, EdgeCost>, EdgeCost)>();

    /// <summary>
    /// A never null list of child nodes and their edge costs. The order is not relevant. Note that this is a DAG, and the cost represents the cost from this node to the child one.
    /// </summary>
    public IList<(Node<T, EdgeCost>, EdgeCost)> ChildNodes { get; } = new List<(Node<T, EdgeCost>, EdgeCost)>();


    /// <summary>
    /// Creates a node.
    /// </summary>
    /// <param name="identity">Cannot be null.</param>
    public Node(T identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        Identity = identity;
    }
}