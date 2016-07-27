using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoUtil
{
    internal struct NuGetPackage : IEquatable<NuGetPackage>
    {
        internal string Name { get; }
        internal string Version { get; }

        internal NuGetPackage(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public static bool operator ==(NuGetPackage left, NuGetPackage right) =>
            Constants.NugetPackageNameComparer.Equals(left.Name, right.Name) &&
            Constants.NugetPackageVersionComparer.Equals(left.Version, right.Version);
        public static bool operator !=(NuGetPackage left, NuGetPackage right) => !(left == right);
        public override bool Equals(object obj) => obj is NuGetPackage && Equals((NuGetPackage)obj);
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
        public override string ToString() => $"{Name}-{Version}";
        public bool Equals(NuGetPackage other) => this == other;
    }
}
