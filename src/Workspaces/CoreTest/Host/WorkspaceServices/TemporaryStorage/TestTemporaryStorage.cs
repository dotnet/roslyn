// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests.TemporaryStorage
{
    public class TestTemporaryStorage : ITemporaryStorage
    {
        private SourceText text;
        private Stream stream;

        public SourceText ReadText(CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfNull(text);
            return text;
        }

        public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(ReadText(cancellationToken));
        }

        public void WriteText(SourceText text, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(text != null || stream != null);
            this.text = text;
        }

        public Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteText(text, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }

        public Stream ReadStream(CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfNull(stream);
            return stream;
        }

        public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(ReadStream(cancellationToken));
        }

        public void WriteStream(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            Contract.ThrowIfTrue(text != null || stream != null);
            this.stream = stream;
        }

        public Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            WriteStream(stream, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }

        public void Dispose()
        {
        }
    }
}
