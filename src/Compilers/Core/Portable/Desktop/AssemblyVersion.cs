// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis
{
    internal readonly struct AssemblyVersion : IEquatable<AssemblyVersion>, IComparable<AssemblyVersion>
    {
        private readonly ushort _major;
        private readonly ushort _minor;
        private readonly ushort _build;
        private readonly ushort _revision;

        public AssemblyVersion(ushort major, ushort minor, ushort build, ushort revision)
        {
            _major = major;
            _minor = minor;
            _build = build;
            _revision = revision;
        }

        public int Major
        {
            get { return _major; }
        }

        public int Minor
        {
            get { return _minor; }
        }

        public int Build
        {
            get { return _build; }
        }

        public int Revision
        {
            get { return _revision; }
        }

        private ulong ToInteger()
        {
            return ((ulong)_major << 48) | ((ulong)_minor << 32) | ((ulong)_build << 16) | _revision;
        }

        public int CompareTo(AssemblyVersion other)
        {
            var left = ToInteger();
            var right = other.ToInteger();
            return (left == right) ? 0 : (left < right) ? -1 : +1;
        }

        public bool Equals(AssemblyVersion other)
        {
            return ToInteger() == other.ToInteger();
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyVersion && Equals((AssemblyVersion)obj);
        }

        public override int GetHashCode()
        {
            return ((_major & 0x000f) << 28) | ((_minor & 0x00ff) << 20) | ((_build & 0x00ff) << 12) | (_revision & 0x0fff);
        }

        public static bool operator ==(AssemblyVersion left, AssemblyVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AssemblyVersion left, AssemblyVersion right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(AssemblyVersion left, AssemblyVersion right)
        {
            return left.ToInteger() < right.ToInteger();
        }

        public static bool operator <=(AssemblyVersion left, AssemblyVersion right)
        {
            return left.ToInteger() <= right.ToInteger();
        }

        public static bool operator >(AssemblyVersion left, AssemblyVersion right)
        {
            return left.ToInteger() > right.ToInteger();
        }

        public static bool operator >=(AssemblyVersion left, AssemblyVersion right)
        {
            return left.ToInteger() >= right.ToInteger();
        }

        /// <summary>
        /// Converts <see cref="Version"/> to <see cref="AssemblyVersion"/>.
        /// </summary>
        /// <exception cref="InvalidCastException">Major, minor, build or revision number are less than 0 or greater than 0xFFFF.</exception>
        public static explicit operator AssemblyVersion(Version version)
        {
            return new AssemblyVersion((ushort)version.Major, (ushort)version.Minor, (ushort)version.Build, (ushort)version.Revision);
        }

        public static explicit operator Version(AssemblyVersion version)
        {
            return new Version(version.Major, version.Minor, version.Build, version.Revision);
        }
    }
}
