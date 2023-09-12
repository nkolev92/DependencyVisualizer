using System.Diagnostics;
using NuGet.Versioning;

namespace Common
{
    [DebuggerDisplay("{Identity}")]

    public class PackageDependencyNode : Node<DependencyNodeIdentity, VersionRange>
    {
        public PackageDependencyNode(DependencyNodeIdentity identity) : base(identity)
        {
        }

        public override string ToString()
        {
            return Identity.ToString();
        }
    }
}