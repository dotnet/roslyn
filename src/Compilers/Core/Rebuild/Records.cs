// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rebuild
{
    public sealed record EmbeddedSourceTextInfo(
        SourceTextInfo SourceTextInfo,
        SourceText SourceText,
        ImmutableArray<byte> CompressedHash);

    public sealed record SourceTextInfo(
        string OriginalSourceFilePath,
        SourceHashAlgorithm HashAlgorithm,
        ImmutableArray<byte> Hash,
        Encoding SourceTextEncoding);

    public sealed record MetadataReferenceInfo(
        string FileName,
        Guid ModuleVersionId,
        string? ExternAlias,
        MetadataImageKind ImageKind,
        bool EmbedInteropTypes,
        int Timestamp,
        int ImageSize);
}
