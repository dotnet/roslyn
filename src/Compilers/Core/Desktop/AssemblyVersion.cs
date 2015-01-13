// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal struct AssemblyVersion : IEquatable<AssemblyVersion>, IComparable<AssemblyVersion>
    {
        private readonly ushort major;
        private readonly ushort minor;
        private readonly ushort build;
        private readonly ushort revision;

        public AssemblyVersion(ushort major, ushort minor, ushort build, ushort revision)
        {
            this.major = major;
            this.minor = minor;
            this.build = build;
            this.revision = revision;
        }

        public int Major
        {
            get { return major; }
        }

        public int Minor
        {
            get { return minor; }
        }

        public int Build
        {
            get { return build; }
        }

        public int Revision
        {
            get { return revision; }
        }

        private ulong ToInteger()
        {
            return ((ulong)major << 48) | ((ulong)minor << 32) | ((ulong)build << 16) | revision;
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
            return ((this.major & 0x000f) << 28) | ((this.minor & 0x00ff) << 20) | ((this.build & 0x00ff) << 12) | (this.revision & 0x0fff);
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
