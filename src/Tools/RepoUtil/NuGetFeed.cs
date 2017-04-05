using System;

namespace RepoUtil
{
    internal struct NuGetFeed : IEquatable<NuGetFeed>
    {
        internal string Name { get; }
        internal Uri Url { get; }

        internal NuGetFeed(string name, Uri url)
        {
            Name = name;
            Url = url;
        }

        public static bool operator ==(NuGetFeed left, NuGetFeed right) =>
            StringComparer.OrdinalIgnoreCase.Equals(left.Name, right.Name) &&
            left.Url == right.Url;
        public static bool operator !=(NuGetFeed left, NuGetFeed right) => !(left == right);
        public override bool Equals(object obj) => obj is NuGetFeed && Equals((NuGetFeed)obj);
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
        public override string ToString() => $"{Name}-{Url}";
        public bool Equals(NuGetFeed other) => this == other;
    }
}
