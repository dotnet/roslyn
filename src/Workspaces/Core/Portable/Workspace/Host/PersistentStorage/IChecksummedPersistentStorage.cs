// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IChecksummedPersistentStorage : IPersistentStorage
    {
        /// <summary>
        /// Reads the existing checksum we have for the solution with the given <paramref name="name"/>,
        /// or <see langword="null"/> if we do not have a checksum persisted.
        /// </summary>
        Task<Checksum> ReadChecksumAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the existing checksum we have for the given <paramref name="project"/> with the given <paramref name="name"/>,
        /// or <see langword="null"/> if we do not have a checksum persisted.
        /// </summary>
        Task<Checksum> ReadChecksumAsync(Project project, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the existing checksum we have for the given <paramref name="document"/> with the given <paramref name="name"/>,
        /// or <see langword="null"/> if we do not have a checksum persisted.
        /// </summary>
        Task<Checksum> ReadChecksumAsync(Document document, string name, CancellationToken cancellationToken = default);

        Task<Checksum> ReadChecksumAsync(ProjectKey project, string name, CancellationToken cancellationToken = default);
        Task<Checksum> ReadChecksumAsync(DocumentKey document, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the solution with the given <paramref name="name"/>.  If <paramref name="checksum"/>
        /// is provided, the persisted checksum must match it.  If there is no such stream with that name, or the
        /// checksums do not match, then <see langword="null"/> will be returned.
        /// </summary>
        Task<Stream> ReadStreamAsync(string name, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the <paramref name="project"/> with the given <paramref name="name"/>.  If <paramref name="checksum"/>
        /// is provided, the persisted checksum must match it.  If there is no such stream with that name, or the
        /// checksums do not match, then <see langword="null"/> will be returned.
        /// </summary>
        Task<Stream> ReadStreamAsync(Project project, string name, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the <paramref name="document"/> with the given <paramref name="name"/>.  If <paramref name="checksum"/>
        /// is provided, the persisted checksum must match it.  If there is no such stream with that name, or the
        /// checksums do not match, then <see langword="null"/> will be returned.
        /// </summary>
        Task<Stream> ReadStreamAsync(Document document, string name, Checksum checksum = null, CancellationToken cancellationToken = default);

        Task<Stream> ReadStreamAsync(ProjectKey project, string name, Checksum checksum = null, CancellationToken cancellationToken = default);
        Task<Stream> ReadStreamAsync(DocumentKey document, string name, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the solution with the given <paramref name="name"/>.  An optional <paramref name="checksum"/>
        /// can be provided to store along with the data.  This can be used along with ReadStreamAsync with future 
        /// reads to ensure the data is only read back if it matches that checksum.
        /// </summary>
        Task<bool> WriteStreamAsync(string name, Stream stream, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the <paramref name="project"/> with the given <paramref name="name"/>.  An optional <paramref name="checksum"/>
        /// can be provided to store along with the data.  This can be used along with ReadStreamAsync with future 
        /// reads to ensure the data is only read back if it matches that checksum.
        /// </summary>
        Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum checksum = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the stream for the <paramref name="document"/> with the given <paramref name="name"/>.  An optional <paramref name="checksum"/>
        /// can be provided to store along with the data.  This can be used along with ReadStreamAsync with future 
        /// reads to ensure the data is only read back if it matches that checksum.
        /// </summary>
        Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum checksum = null, CancellationToken cancellationToken = default);
    }
}
