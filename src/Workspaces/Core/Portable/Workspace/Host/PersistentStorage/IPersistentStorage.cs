// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
