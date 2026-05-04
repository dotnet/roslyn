// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal sealed class NoOpPersistentStorage(SolutionKey solutionKey) : IChecksummedPersistentStorage
{
    public SolutionKey SolutionKey => solutionKey;

    public static IChecksummedPersistentStorage GetOrThrow(SolutionKey solutionKey, bool throwOnFailure)
        => throwOnFailure
            ? throw new InvalidOperationException("Database was not supported")
            : new NoOpPersistentStorage(solutionKey);

    public async Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken)
        => false;

    public async Task<bool> ChecksumMatchesAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken)
        => false;

    public async Task<bool> ChecksumMatchesAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken)
        => false;

    public async Task<bool> ChecksumMatchesAsync(ProjectKey project, string name, Checksum checksum, CancellationToken cancellationToken)
        => false;

    public async Task<bool> ChecksumMatchesAsync(DocumentKey document, string name, Checksum checksum, CancellationToken cancellationToken)
        => false;

    public async Task<Stream?> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken)
        => null;

    public async Task<Stream?> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken)
        => null;

    public async Task<Stream?> ReadStreamAsync(string name, CancellationToken cancellationToken)
        => null;

    public async Task<Stream?> ReadStreamAsync(string name, Checksum? checksum, CancellationToken cancellationToken)
        => null;

    public async Task<Stream?> ReadStreamAsync(Project project, string name, Checksum? checksum, CancellationToken cancellationToken)
        => null;

    public async Task<Stream?> ReadStreamAsync(Document document, string name, Checksum? checksum, CancellationToken cancellationToken)
        => null;

    public async Task<Stream?> ReadStreamAsync(ProjectKey project, string name, Checksum? checksum, CancellationToken cancellationToken)
        => null;

    public async Task<Stream?> ReadStreamAsync(DocumentKey document, string name, Checksum? checksum, CancellationToken cancellationToken)
        => null;

    public async Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken)
        => false;

    public async Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken)
        => false;

    public async Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken)
        => false;

    public async Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => false;

    public async Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => false;

    public async Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => false;

    public async Task<bool> WriteStreamAsync(ProjectKey projectKey, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => false;

    public async Task<bool> WriteStreamAsync(DocumentKey documentKey, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => false;

    public readonly struct TestAccessor
    {
        public static IChecksummedPersistentStorage GetStorageInstance(SolutionKey solutionKey) => new NoOpPersistentStorage(solutionKey);
    }
}
