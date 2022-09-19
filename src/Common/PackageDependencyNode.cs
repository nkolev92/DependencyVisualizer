using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Common
{
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
