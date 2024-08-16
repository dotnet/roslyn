// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.Host;

internal interface IChecksummedPersistentStorage : IPersistentStorage
{
    /// <summary>
    /// The solution this is a storage instance for.
    /// </summary>
    SolutionKey SolutionKey { get; }

    /// <summary>
    /// <see langword="true"/> if the data we have for the solution with the given <paramref name="name"/> has the
    /// provided <paramref name="checksum"/>.
    /// </summary>
    Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken = default);

    /// <summary>
    /// <see langword="true"/> if the data we have for the given <paramref name="project"/> with the given <paramref
    /// name="name"/> has the provided <paramref name="checksum"/>.
    /// </summary>
    Task<bool> ChecksumMatchesAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken = default);

    /// <summary>
    /// <see langword="true"/> if the data we have for the given <paramref name="document"/> with the given <paramref
    /// name="name"/> has the provided <paramref name="checksum"/>.
    /// </summary>
    Task<bool> ChecksumMatchesAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken = default);

    Task<bool> ChecksumMatchesAsync(ProjectKey project, string name, Checksum checksum, CancellationToken cancellationToken = default);
    Task<bool> ChecksumMatchesAsync(DocumentKey document, string name, Checksum checksum, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the stream for the solution with the given <paramref name="name"/>.  If <paramref name="checksum"/>
    /// is provided, the persisted checksum must match it.  If there is no such stream with that name, or the
    /// checksums do not match, then <see langword="null"/> will be returned.
    /// </summary>
    Task<Stream?> ReadStreamAsync(string name, Checksum? checksum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the stream for the <paramref name="project"/> with the given <paramref name="name"/>.  If <paramref name="checksum"/>
    /// is provided, the persisted checksum must match it.  If there is no such stream with that name, or the
    /// checksums do not match, then <see langword="null"/> will be returned.
    /// </summary>
    Task<Stream?> ReadStreamAsync(Project project, string name, Checksum? checksum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the stream for the <paramref name="document"/> with the given <paramref name="name"/>.  If <paramref name="checksum"/>
    /// is provided, the persisted checksum must match it.  If there is no such stream with that name, or the
    /// checksums do not match, then <see langword="null"/> will be returned.
    /// </summary>
    Task<Stream?> ReadStreamAsync(Document document, string name, Checksum? checksum = null, CancellationToken cancellationToken = default);

    Task<Stream?> ReadStreamAsync(ProjectKey project, string name, Checksum? checksum = null, CancellationToken cancellationToken = default);
    Task<Stream?> ReadStreamAsync(DocumentKey document, string name, Checksum? checksum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the stream for the solution with the given <paramref name="name"/>.  An optional <paramref
    /// name="checksum"/> can be provided to store along with the data.  This can be used along with ReadStreamAsync
    /// with future reads to ensure the data is only read back if it matches that checksum.
    /// <para>
    /// Returns <see langword="true"/> if the data was successfully persisted to the storage subsystem.  Subsequent
    /// calls to read the same keys should succeed if called within the same session.
    /// </para>
    /// </summary>
    Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the stream for the <paramref name="project"/> with the given <paramref name="name"/>.  An optional
    /// <paramref name="checksum"/> can be provided to store along with the data.  This can be used along with
    /// ReadStreamAsync with future reads to ensure the data is only read back if it matches that checksum.
    /// <para>
    /// Returns <see langword="true"/> if the data was successfully persisted to the storage subsystem.  Subsequent
    /// calls to read the same keys should succeed if called within the same session.
    /// </para>
    /// </summary>
    Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum? checksum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the stream for the <paramref name="document"/> with the given <paramref name="name"/>.  An optional
    /// <paramref name="checksum"/> can be provided to store along with the data.  This can be used along with
    /// ReadStreamAsync with future reads to ensure the data is only read back if it matches that checksum.
    /// <para>
    /// Returns <see langword="true"/> if the data was successfully persisted to the storage subsystem.  Subsequent
    /// calls to read the same keys should succeed if called within the same session.
    /// </para>
    /// </summary>
    Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum? checksum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> if the data was successfully persisted to the storage subsystem.  Subsequent
    /// calls to read the same keys should succeed if called within the same session.
    /// </summary>
    Task<bool> WriteStreamAsync(ProjectKey projectKey, string name, Stream stream, Checksum? checksum = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <see langword="true"/> if the data was successfully persisted to the storage subsystem.  Subsequent
    /// calls to read the same keys should succeed if called within the same session.
    /// </summary>
    Task<bool> WriteStreamAsync(DocumentKey documentKey, string name, Stream stream, Checksum? checksum = null, CancellationToken cancellationToken = default);
}
