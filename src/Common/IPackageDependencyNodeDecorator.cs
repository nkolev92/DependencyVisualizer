namespace Common
{
    /// <summary>
    /// A decorator for <see cref="PackageDependencyNode"/> that allows for additional information to be added to the node.
    /// </summary>
    public interface IPackageDependencyNodeDecorator
    {
        /// <summary>
        /// Adds information to the node.
        /// </summary>
        Task DecorateAsync(PackageDependencyNode dependencyNode, CancellationToken cancellationToken);
    }
}
