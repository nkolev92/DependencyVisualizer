namespace Common
{
    public interface IPackageDependencyNodeDecorator
    {
        Task DecorateAsync(PackageDependencyNode dependencyNode, CancellationToken cancellationToken);
    }
}