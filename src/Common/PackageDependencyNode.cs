using NuGet.Packaging.Core;
using NuGet.Versioning;
using System.Diagnostics;

namespace Common
{
    [DebuggerDisplay("{Identity}")]

    public class PackageDependencyNode : Node<PackageIdentity, VersionRange>
    {
        public PackageDependencyNode(PackageIdentity identity) : base(identity)
        {
        }

        public override string ToString()
        {
            return Identity.ToString();
        }
    }
}
