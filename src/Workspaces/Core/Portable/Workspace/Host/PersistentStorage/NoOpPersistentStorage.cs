﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal class NoOpPersistentStorage : IChecksummedPersistentStorage
    {
        public static readonly IChecksummedPersistentStorage Instance = new NoOpPersistentStorage();

        private NoOpPersistentStorage()
        {
        }

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync()
        {
            return ValueTaskFactory.CompletedTask;
        }

        public Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> ChecksumMatchesAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> ChecksumMatchesAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> ChecksumMatchesAsync(ProjectKey project, string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> ChecksumMatchesAsync(DocumentKey document, string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<Stream?> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken)
            => SpecializedTasks.Null<Stream>();

        public Task<Stream?> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken)
            => SpecializedTasks.Null<Stream>();

        public Task<Stream?> ReadStreamAsync(string name, CancellationToken cancellationToken)
            => SpecializedTasks.Null<Stream>();

        public Task<Stream?> ReadStreamAsync(string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.Null<Stream>();

        public Task<Stream?> ReadStreamAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.Null<Stream>();

        public Task<Stream?> ReadStreamAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.Null<Stream>();

        public Task<Stream?> ReadStreamAsync(ProjectKey project, string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.Null<Stream>();

        public Task<Stream?> ReadStreamAsync(DocumentKey document, string name, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.Null<Stream>();

        public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> WriteStreamAsync(string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> WriteStreamAsync(ProjectKey projectKey, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;

        public Task<bool> WriteStreamAsync(DocumentKey documentKey, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
            => SpecializedTasks.False;
    }
}
