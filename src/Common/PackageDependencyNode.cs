using System.Diagnostics;
using NuGet.Versioning;

namespace Common
{
    /// <summary>
    /// Represents a package dependency node.
    /// </summary>
    [DebuggerDisplay("{Identity}")]
    public class PackageDependencyNode : Node<DependencyNodeIdentity, VersionRange>
    {
        /// <summary>
        /// Create a node.
        /// </summary>
        public PackageDependencyNode(DependencyNodeIdentity identity) : base(identity)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Identity.ToString();
        }
    }
}