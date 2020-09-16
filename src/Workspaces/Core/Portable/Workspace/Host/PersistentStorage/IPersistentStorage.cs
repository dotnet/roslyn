// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    public interface IPersistentStorage : IDisposable
    {
        Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default);

        Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default);
        Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default);

        Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default);
        Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default);
        Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default);
    }
}
