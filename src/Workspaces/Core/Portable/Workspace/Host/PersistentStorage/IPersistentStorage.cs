// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <remarks>
    /// Instances of <see cref="IPersistentStorage"/> support both synchronous and asynchronous disposal.  Asynchronous
    /// disposal should always be preferred as the implementation of synchronous disposal may end up blocking the caller
    /// on async work.
    /// </remarks>
    public interface IPersistentStorage : IDisposable, IAsyncDisposable
    {
        Task<Stream?> ReadStreamAsync(string name, CancellationToken cancellationToken = default);
        Task<Stream?> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default);
        Task<Stream?> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns <see langword="true"/> if the data was successfully persisted to the storage subsystem.  Subsequent
        /// calls to read the same keys should succeed if called within the same session.
        /// </summary>
        Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns <see langword="true"/> if the data was successfully persisted to the storage subsystem.  Subsequent
        /// calls to read the same keys should succeed if called within the same session.
        /// </summary>
        Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns <see langword="true"/> if the data was successfully persisted to the storage subsystem.  Subsequent
        /// calls to read the same keys should succeed if called within the same session.
        /// </summary>
        Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default);
    }
}
