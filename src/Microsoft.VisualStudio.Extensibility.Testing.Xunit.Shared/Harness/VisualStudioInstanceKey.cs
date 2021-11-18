// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public readonly struct VisualStudioInstanceKey : IEquatable<VisualStudioInstanceKey>
    {
        public static readonly VisualStudioInstanceKey Unspecified = new(VisualStudioVersion.Unspecified, rootSuffix: string.Empty);

        public VisualStudioInstanceKey(VisualStudioVersion version, string rootSuffix)
        {
            Version = version;
            RootSuffix = rootSuffix;
        }

        public VisualStudioVersion Version { get; }

        public string RootSuffix { get; }

        public static bool operator ==(VisualStudioInstanceKey left, VisualStudioInstanceKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VisualStudioInstanceKey left, VisualStudioInstanceKey right)
        {
            return !(left == right);
        }

        public override bool Equals(object? obj)
        {
            return obj is VisualStudioInstanceKey other
                && Equals(other);
        }

        public bool Equals(VisualStudioInstanceKey other)
        {
            return Version == other.Version
                && RootSuffix == other.RootSuffix;
        }

        public override int GetHashCode()
        {
            var hashCode = 223772477;
            hashCode = (hashCode * -1521134295) + Version.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(RootSuffix);
            return hashCode;
        }

        public string SerializeToString()
        {
            return $"{(int)Version};{RootSuffix}";
        }

        public static VisualStudioInstanceKey DeserializeFromString(string s)
        {
            var semicolon = s.IndexOf(';');
            return new VisualStudioInstanceKey(
                version: (VisualStudioVersion)int.Parse(s.Substring(0, semicolon)),
                rootSuffix: s.Substring(semicolon + 1));
        }
    }
}
