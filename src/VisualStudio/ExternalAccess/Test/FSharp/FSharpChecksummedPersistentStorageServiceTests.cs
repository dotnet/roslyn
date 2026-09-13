// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Storage;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Storage;
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
    private static (Document document, IFSharpChecksummedPersistentStorage storage) CreateDocumentAndStorage()
    {
        using var workspace = new AdhocWorkspace();
        var projectId = workspace.AddProject("Project", LanguageNames.CSharp).Id;
        var document = workspace.AddDocument(projectId, "Test.cs", SourceText.From(""));

        // The constructor is [Obsolete(error: true)] to keep production code from bypassing MEF; that is a
        // compile-time guard only, so a test can still reach it through reflection.
        var service = (FSharpChecksummedPersistentStorageService)Activator.CreateInstance(typeof(FSharpChecksummedPersistentStorageService), nonPublic: true)!;

        var storage = service.GetStorageAsync(document.Project.Solution, CancellationToken.None).AsTask().GetAwaiter().GetResult();
        return (document, storage);
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
