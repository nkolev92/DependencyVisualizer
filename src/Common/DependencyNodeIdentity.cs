using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Common
{
    /// <summary>
    /// Represents a dependency node identity, can be a package or a project.
    /// </summary>
    public class DependencyNodeIdentity : PackageIdentity
    {
        /// <summary>
        /// Type of dependency
        /// </summary>
        public DependencyType Type { get; }

        /// <summary>
        /// Whether the dependency is vulnerable
        /// </summary>
        public bool Vulnerable { get; set; }

        /// <summary>
        /// Whether the dependency is deprecated
        /// </summary>
        public bool Deprecated { get; set; }

        /// <summary>
        /// Create a dependency node identity.
        /// </summary>
        public DependencyNodeIdentity(string id, NuGetVersion version, DependencyType type) : base(id, version)
        {
            Type = type;
        }
    }
}