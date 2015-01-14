// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal class NoOpPersistentStorage : IPersistentStorage
    {
        public void Dispose()
        {
        }

        public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.Default<Stream>();
        }

        public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.Default<Stream>();
        }

        public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.Default<Stream>();
        }

        public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.False;
        }

        public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.False;
        }

        public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            return SpecializedTasks.False;
        }
    }
}
