// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;

namespace BuildValidator
{
    internal readonly struct MetadataReferenceInfo
    {
        public readonly int Timestamp;
        public readonly int ImageSize;
        public readonly string Name;
        public readonly FileInfo FileInfo;
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
            FileInfo = new FileInfo(name);
        }

        public override string ToString()
        {
            return $"{Name}::{Mvid}::{Timestamp}";
        }
    }
}
