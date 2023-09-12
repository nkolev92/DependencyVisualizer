using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Common
{
    public class DependencyNodeIdentity : PackageIdentity
    {
        public DependencyType Type { get; }

        public bool Vulnerable { get; set; }

        public bool Deprecated { get; set; }

        public DependencyNodeIdentity(string id, NuGetVersion version, DependencyType type) : base(id, version)
        {
            Type = type;
        }
    }
}