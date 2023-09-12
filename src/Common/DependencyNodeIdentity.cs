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

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Type, Vulnerable, Deprecated);
        }

        public override bool Equals(object? obj)
        {
            return obj is DependencyNodeIdentity identity &&
                   base.Equals(obj) &&
                   Type == identity.Type &&
                   Vulnerable == identity.Vulnerable &&
                   Deprecated == identity.Deprecated;
        }
    }
}