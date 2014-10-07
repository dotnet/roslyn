// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal struct MetadataInfo : IEquatable<MetadataInfo>
    {
        public readonly string Path;
        public readonly MetadataReferenceProperties Properties;

        public MetadataInfo(string path, MetadataReferenceProperties properties = default(MetadataReferenceProperties))
        {
            Path = path;
            Properties = properties;
        }

        public override bool Equals(object obj)
        {
            return obj is MetadataInfo && base.Equals((MetadataInfo)obj);
        }

        public bool Equals(MetadataInfo other)
        {
            return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
                && Properties == other.Properties;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(
                Path != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Path) : 0, Properties.GetHashCode());
        }

        public static bool operator ==(MetadataInfo left, MetadataInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MetadataInfo left, MetadataInfo right)
        {
            return !left.Equals(right);
        }
    }
}