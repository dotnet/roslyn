using System;

namespace RepoUtil
{
    internal struct NuGetPackage : IEquatable<NuGetPackage>
    {
        internal string Name { get; }
        internal string Version { get; }
        internal string GenerateNameOpt { get; }

        internal NuGetPackage(string name, string version)
            :this(name, version, generateName: null) { }

        internal NuGetPackage(string name, string version, string generateName)
        {
            Name = name;
            Version = version;
            GenerateNameOpt = generateName;
        }

        public static bool operator ==(NuGetPackage left, NuGetPackage right) =>
            Constants.NugetPackageNameComparer.Equals(left.Name, right.Name) &&
            Constants.NugetPackageVersionComparer.Equals(left.Version, right.Version) &&
            left.GenerateNameOpt == right.GenerateNameOpt;
        public static bool operator !=(NuGetPackage left, NuGetPackage right) => !(left == right);
        public override bool Equals(object obj) => obj is NuGetPackage && Equals((NuGetPackage)obj);
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
        public override string ToString() => GenerateNameOpt == null
            ? $"{Name}-{Version}"
            : $"{Name}-{Version}, {GenerateNameOpt}";
        public bool Equals(NuGetPackage other) => this == other;
    }
}
