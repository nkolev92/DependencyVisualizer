using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Common
{
    public class DependencyNodeIdentity : PackageIdentity
    {
        public DependencyType Type { get; }

        public DependencyNodeIdentity(string id, NuGetVersion version, DependencyType type) : base(id, version)
        {
            Type = type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Type);
        }

        public override bool Equals(object? obj)
        {
            return obj is DependencyNodeIdentity identity &&
                   base.Equals(obj) &&
                   Type == identity.Type;
        }
    }
}
