// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public readonly struct VisualStudioInstanceKey : IEquatable<VisualStudioInstanceKey>
    {
        public static readonly VisualStudioInstanceKey Unspecified = new(VisualStudioVersion.Unspecified, rootSuffix: string.Empty, maxAttempts: 1);

        public VisualStudioInstanceKey(VisualStudioVersion version, string rootSuffix, int maxAttempts)
        {
            Version = version;
            RootSuffix = rootSuffix;
            MaxAttempts = maxAttempts;
        }

        public VisualStudioVersion Version { get; }

        public string RootSuffix { get; }

        public int MaxAttempts { get; }

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
                && RootSuffix == other.RootSuffix
                && MaxAttempts == other.MaxAttempts;
        }

        public override int GetHashCode()
        {
            var hashCode = 223772477;
            hashCode = (hashCode * -1521134295) + Version.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(RootSuffix);
            hashCode = (hashCode * -1521134295) + MaxAttempts.GetHashCode();
            return hashCode;
        }

        public string SerializeToString()
        {
            return $"{(int)Version};{RootSuffix};{MaxAttempts}";
        }

        public static VisualStudioInstanceKey DeserializeFromString(string s)
        {
            var semicolon1 = s.IndexOf(';');
            var semicolon2 = s.IndexOf(';', semicolon1 + 1);
            return new VisualStudioInstanceKey(
                version: (VisualStudioVersion)int.Parse(s.Substring(0, semicolon1)),
                rootSuffix: s.Substring(semicolon1 + 1, semicolon2 - semicolon1 - 1),
                maxAttempts: int.Parse(s.Substring(semicolon2 + 1)));
        }
    }
}
