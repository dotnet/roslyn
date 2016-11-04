// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Packaging;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal class PackageWithAssemblyInfo : PackageInfo, IEquatable<PackageWithAssemblyInfo>, IComparable<PackageWithAssemblyInfo>
    {
        public readonly string Version;

        public PackageWithAssemblyInfo(
            PackageSource source,
            string packageName,
            string version,
            int rank)
            : base(source, packageName, rank)
        {
            Version = string.IsNullOrWhiteSpace(version) ? null : version;
        }

        public override int GetHashCode()
            => PackageName.GetHashCode();

        public override bool Equals(object obj)
            => Equals((PackageWithAssemblyInfo)obj);

        public bool Equals(PackageWithAssemblyInfo other)
            => PackageName.Equals(other.PackageName);

        public int CompareTo(PackageWithAssemblyInfo other)
        {
            var diff = Rank - other.Rank;
            if (diff != 0)
            {
                return -diff;
            }

            return PackageName.CompareTo(other.PackageName);
        }
    }
}