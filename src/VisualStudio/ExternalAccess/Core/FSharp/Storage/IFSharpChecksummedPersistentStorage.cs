// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Storage;

/// <summary>
/// The per-document part of the storage Roslyn keeps its own indices in, so that F# can keep the data it computes for
/// a document between sessions as Roslyn does.
/// </summary>
/// <remarks>
/// A checksum is the bytes of a Roslyn checksum: its first 16 are used, and fewer are rejected. Where one is optional,
/// <see langword="default"/> stands for none.
/// </remarks>
internal interface IFSharpChecksummedPersistentStorage
{
    /// <summary>
    /// <see langword="true"/> if the data stored for <paramref name="document"/> under <paramref name="name"/> was
    /// written with <paramref name="checksum"/>.
    /// </summary>
    Task<bool> ChecksumMatchesAsync(Document document, string name, ImmutableArray<byte> checksum, CancellationToken cancellationToken);

    /// <summary>
    /// The data stored for <paramref name="document"/> under <paramref name="name"/>, or <see langword="null"/> if there
    /// is none or <paramref name="checksum"/> is given and differs from the one it was written with.
    /// </summary>
    Task<Stream?> ReadStreamAsync(Document document, string name, ImmutableArray<byte> checksum, CancellationToken cancellationToken);

    /// <summary>
    /// Stores <paramref name="stream"/> for <paramref name="document"/> under <paramref name="name"/>, with the
    /// <paramref name="checksum"/> a later read compares. <see langword="true"/> if it was stored.
    /// </summary>
    Task<bool> WriteStreamAsync(Document document, string name, Stream stream, ImmutableArray<byte> checksum, CancellationToken cancellationToken);
}
