// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Packaging
{
    internal struct PackageSource : IEquatable<PackageSource>
    {
        public readonly string Name;
        public readonly string Source;

        public PackageSource(string name, string source)
        {
            Name = name;
            Source = source;
        }

        public override bool Equals(object obj)
            => Equals((PackageSource)obj);

        public bool Equals(PackageSource other)
            => Name == other.Name && Source == other.Source;

        public override int GetHashCode()
            => Hash.Combine(Name, Source.GetHashCode());
    }
}