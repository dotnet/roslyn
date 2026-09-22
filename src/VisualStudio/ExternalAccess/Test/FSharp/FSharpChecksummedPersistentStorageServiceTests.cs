// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Storage;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Storage;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests;

/// <summary>
/// <see cref="FSharpChecksummedPersistentStorageService"/> bridges an <see cref="ImmutableArray{T}"/> checksum to
/// Roslyn's <see cref="Checksum"/>. <see cref="Checksum.From(ImmutableArray{byte})"/> truncates an array longer than
/// 16 bytes and throws on one shorter, for every method. <c>ReadStreamAsync</c> and <c>WriteStreamAsync</c> also
/// treat a <em>default</em> array as "no checksum given" and pass <see langword="null"/> through, because the
/// underlying storage takes an optional <c>Checksum?</c> there. <c>ChecksumMatchesAsync</c> has no such case: the
/// storage it calls takes a non-optional <c>Checksum</c> — asking whether something matches an unspecified checksum
/// is meaningless — so a default array reaches it exactly like a too-short one, and throws.
///
/// These tests exercise that boundary against a workspace with no persistent storage backing it, so a call that
/// gets past the checksum conversion always reaches the same no-op answer.
/// </summary>
public sealed class FSharpChecksummedPersistentStorageServiceTests
{
    // The constructor is [Obsolete(error: true)] to keep production code from bypassing MEF; that is a
    // compile-time guard only, so a test can still reach it through reflection.
    private static FSharpChecksummedPersistentStorageService CreateService()
        => (FSharpChecksummedPersistentStorageService)Activator.CreateInstance(typeof(FSharpChecksummedPersistentStorageService), nonPublic: true)!;

    private static (Document document, IFSharpChecksummedPersistentStorage storage) CreateDocumentAndStorage()
    {
        using var workspace = new AdhocWorkspace();
        var projectId = workspace.AddProject("Project", LanguageNames.CSharp).Id;
        var document = workspace.AddDocument(projectId, "Test.cs", SourceText.From(""));
        var storage = CreateService().GetStorageAsync(document.Project.Solution, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        return (document, storage);
    }

    /// Wraps <paramref name="storage"/> in the adapter directly, bypassing <see cref="FSharpChecksummedPersistentStorageService.GetStorageAsync"/>
    /// and the real Roslyn storage it resolves, so the wrapper's own forwarding can be checked against a fake.
    private static IFSharpChecksummedPersistentStorage CreateAdapter(IChecksummedPersistentStorage storage)
    {
        var adapterType = typeof(FSharpChecksummedPersistentStorageService).GetNestedType("FSharpChecksummedPersistentStorage", BindingFlags.NonPublic)!;
        return (IFSharpChecksummedPersistentStorage)Activator.CreateInstance(adapterType, [storage])!;
    }

    /// The <see cref="IChecksummedPersistentStorage"/> a live adapter wraps, reached through its one field of that type.
    private static IChecksummedPersistentStorage UnderlyingStorage(IFSharpChecksummedPersistentStorage storage)
    {
        var field = storage.GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(f => typeof(IChecksummedPersistentStorage).IsAssignableFrom(f.FieldType));

        return (IChecksummedPersistentStorage)field.GetValue(storage)!;
    }

    /// Records the arguments of the three Document-level calls the adapter makes and nothing else; every other
    /// member of <see cref="IChecksummedPersistentStorage"/> is unreachable through <see cref="IFSharpChecksummedPersistentStorage"/>.
    private sealed class RecordingStorage : IChecksummedPersistentStorage
    {
        public SolutionKey SolutionKey { get; init; }
        public bool ChecksumMatchesResult;
        public Stream? ReadResult;
        public bool WriteResult;

        public (Document Document, string Name, Checksum Checksum, CancellationToken CancellationToken)? LastChecksumMatches;
        public (Document Document, string Name, Checksum? Checksum, CancellationToken CancellationToken)? LastRead;
        public (Document Document, string Name, Stream Stream, Checksum? Checksum, CancellationToken CancellationToken)? LastWrite;

        public Task<bool> ChecksumMatchesAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken)
        {
            LastChecksumMatches = (document, name, checksum, cancellationToken);
            return Task.FromResult(ChecksumMatchesResult);
        }

        public Task<Stream?> ReadStreamAsync(Document document, string name, Checksum? checksum, CancellationToken cancellationToken)
        {
            LastRead = (document, name, checksum, cancellationToken);
            return Task.FromResult(ReadResult);
        }

        public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        {
            LastWrite = (document, name, stream, checksum, cancellationToken);
            return Task.FromResult(WriteResult);
        }

        // The adapter never reaches these: IFSharpChecksummedPersistentStorage has no solution-, project- or
        // key-level members, and no checksum-less overload.
        private static NotSupportedException Unreachable() => new("not reachable through IFSharpChecksummedPersistentStorage");

        public Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<bool> ChecksumMatchesAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<bool> ChecksumMatchesAsync(ProjectKey project, string name, Checksum checksum, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<bool> ChecksumMatchesAsync(DocumentKey document, string name, Checksum checksum, CancellationToken cancellationToken = default) => throw Unreachable();

        public Task<Stream?> ReadStreamAsync(string name, Checksum? checksum = null, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<Stream?> ReadStreamAsync(Project project, string name, Checksum? checksum = null, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<Stream?> ReadStreamAsync(ProjectKey project, string name, Checksum? checksum = null, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<Stream?> ReadStreamAsync(DocumentKey document, string name, Checksum? checksum = null, CancellationToken cancellationToken = default) => throw Unreachable();

        public Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum = null, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum? checksum = null, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<bool> WriteStreamAsync(ProjectKey projectKey, string name, Stream stream, Checksum? checksum = null, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<bool> WriteStreamAsync(DocumentKey documentKey, string name, Stream stream, Checksum? checksum = null, CancellationToken cancellationToken = default) => throw Unreachable();

        public Task<Stream?> ReadStreamAsync(string name, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<Stream?> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<Stream?> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default) => throw Unreachable();
        public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default) => throw Unreachable();
    }

    [Fact]
    public async Task AdapterForwardsTheDocumentNameChecksumStreamAndCancellationTokenItIsGiven()
    {
        using var workspace = new AdhocWorkspace();
        var projectId = workspace.AddProject("Project", LanguageNames.CSharp).Id;
        var document = workspace.AddDocument(projectId, "Test.cs", SourceText.From(""));
        var checksumBytes = Enumerable.Range(0, 16).Select(i => (byte)i).ToImmutableArray();
        var checksum = Checksum.From(checksumBytes);
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;

        var recording = new RecordingStorage();
        var storage = CreateAdapter(recording);

        using var readResult = new MemoryStream();
        recording.ReadResult = readResult;
        var actualReadResult = await storage.ReadStreamAsync(document, "read", checksumBytes, cancellationToken);
        Assert.Same(readResult, actualReadResult);
        Assert.Equal((document, "read", checksum, cancellationToken), recording.LastRead);

        using var writeStream = new MemoryStream();
        recording.WriteResult = true;
        Assert.True(await storage.WriteStreamAsync(document, "write", writeStream, checksumBytes, cancellationToken));
        Assert.Equal((document, "write", writeStream, checksum, cancellationToken), recording.LastWrite);

        recording.ChecksumMatchesResult = true;
        Assert.True(await storage.ChecksumMatchesAsync(document, "match", checksumBytes, cancellationToken));
        Assert.Equal((document, "match", checksum, cancellationToken), recording.LastChecksumMatches);
    }

    [Fact]
    public async Task GetStorageAsyncOpensEachSolutionUnderItsOwnKey()
    {
        using var workspaceA = new AdhocWorkspace();
        var solutionA = workspaceA.AddProject("A", LanguageNames.CSharp).Solution;

        using var workspaceB = new AdhocWorkspace();
        var solutionB = workspaceB.AddProject("B", LanguageNames.CSharp).Solution;

        var service = CreateService();
        var storageA = await service.GetStorageAsync(solutionA, CancellationToken.None);
        var storageB = await service.GetStorageAsync(solutionB, CancellationToken.None);

        Assert.Equal(SolutionKey.ToSolutionKey(solutionA), UnderlyingStorage(storageA).SolutionKey);
        Assert.Equal(SolutionKey.ToSolutionKey(solutionB), UnderlyingStorage(storageB).SolutionKey);
        Assert.NotEqual(solutionA.Id, solutionB.Id);
    }

    [Fact]
    public async Task DefaultChecksumIsOmittedByReadAndWrite()
    {
        var (document, storage) = CreateDocumentAndStorage();

        Assert.Null(await storage.ReadStreamAsync(document, "name", default, CancellationToken.None));

        using var stream = new MemoryStream();
        Assert.False(await storage.WriteStreamAsync(document, "name", stream, default, CancellationToken.None));
    }

    [Fact]
    public async Task DefaultChecksumHasNoMatchToAskChecksumMatchesAsync()
    {
        var (document, storage) = CreateDocumentAndStorage();

        await Assert.ThrowsAsync<ArgumentException>(
            () => storage.ChecksumMatchesAsync(document, "name", default, CancellationToken.None));
    }

    [Fact]
    public async Task SixteenByteChecksumIsAccepted()
    {
        var (document, storage) = CreateDocumentAndStorage();
        var checksum = ImmutableArray.Create(new byte[16]);

        Assert.False(await storage.ChecksumMatchesAsync(document, "name", checksum, CancellationToken.None));
        Assert.Null(await storage.ReadStreamAsync(document, "name", checksum, CancellationToken.None));

        using var stream = new MemoryStream();
        Assert.False(await storage.WriteStreamAsync(document, "name", stream, checksum, CancellationToken.None));
    }

    [Fact]
    public async Task LongerChecksumIsTruncatedNotRejected()
    {
        var (document, storage) = CreateDocumentAndStorage();
        var checksum = ImmutableArray.Create(new byte[24]);

        Assert.False(await storage.ChecksumMatchesAsync(document, "name", checksum, CancellationToken.None));
        Assert.Null(await storage.ReadStreamAsync(document, "name", checksum, CancellationToken.None));

        using var stream = new MemoryStream();
        Assert.False(await storage.WriteStreamAsync(document, "name", stream, checksum, CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    public async Task ShorterNonDefaultChecksumThrowsOnEveryMethod(int length)
    {
        var (document, storage) = CreateDocumentAndStorage();
        var checksum = ImmutableArray.Create(new byte[length]);

        await Assert.ThrowsAsync<ArgumentException>(
            () => storage.ChecksumMatchesAsync(document, "name", checksum, CancellationToken.None));

        await Assert.ThrowsAsync<ArgumentException>(
            () => storage.ReadStreamAsync(document, "name", checksum, CancellationToken.None));

        using var stream = new MemoryStream();

        await Assert.ThrowsAsync<ArgumentException>(
            () => storage.WriteStreamAsync(document, "name", stream, checksum, CancellationToken.None));
    }
}
