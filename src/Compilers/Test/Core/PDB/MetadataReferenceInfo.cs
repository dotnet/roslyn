// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Roslyn.Test.Utilities.PDB
{
    internal readonly struct MetadataReferenceInfo
    {
        public readonly int Timestamp;
        public readonly int ImageSize;
        public readonly string Name;
        public readonly Guid Mvid;
        public readonly ImmutableArray<string> ExternAliases;
        public readonly MetadataImageKind Kind;
        public readonly bool EmbedInteropTypes;

        public MetadataReferenceInfo(
            int timestamp,
            int imageSize,
            string name,
            Guid mvid,
            ImmutableArray<string> externAliases,
            MetadataImageKind kind,
            bool embedInteropTypes)
        {
            Timestamp = timestamp;
            ImageSize = imageSize;
            Name = name;
            Mvid = mvid;
            ExternAliases = externAliases;
            Kind = kind;
            EmbedInteropTypes = embedInteropTypes;
        }

        internal void AssertEqual(MetadataReferenceInfo other)
        {
            Assert.Equal(Name, other.Name);
            Assert.Equal(Timestamp, other.Timestamp);
            Assert.Equal(ImageSize, other.ImageSize);
            Assert.Equal(Mvid, other.Mvid);
            Assert.Equal(ExternAliases, other.ExternAliases);
            Assert.Equal(Kind, other.Kind);
            Assert.Equal(EmbedInteropTypes, other.EmbedInteropTypes);
        }
    }
}
