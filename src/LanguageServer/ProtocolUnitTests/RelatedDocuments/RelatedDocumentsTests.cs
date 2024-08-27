// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RelatedDocuments;

public sealed class RelatedDocumentsTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    private static async Task<VSInternalRelatedDocumentReport[]> RunGetRelatedDocumentsAsync(
        TestLspServer testLspServer,
        Uri uri,
        string? previousResultId = null,
        bool useProgress = false)
    {
        BufferedProgress<VSInternalRelatedDocumentReport[]>? progress = useProgress ? BufferedProgress.Create<VSInternalRelatedDocumentReport[]>(null) : null;
        var spans = await testLspServer.ExecuteRequestAsync<VSInternalRelatedDocumentParams, VSInternalRelatedDocumentReport[]>(
            VSInternalMethods.CopilotRelatedDocumentsName,
            new VSInternalRelatedDocumentParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                PreviousResultId = previousResultId,
                PartialResultToken = progress,
            },
            CancellationToken.None).ConfigureAwait(false);

        if (useProgress)
        {
            Assert.Null(spans);
            spans = progress!.Value.GetFlattenedValues();
        }

        AssertEx.NotNull(spans);
        return spans;
    }

    [Theory, CombinatorialData]
    public async Task ReferenceNoDocuments(bool mutatingLspWorkspace, bool useProgress)
    {
        var markup1 = """
            class X
            {
            }
            """;

        var markup2 = """
            class Y
            {
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync([markup1, markup2], mutatingLspWorkspace);

        var project = testLspServer.TestWorkspace.CurrentSolution.Projects.Single();
        var results = await RunGetRelatedDocumentsAsync(
            testLspServer,
            project.Documents.First().GetURI(),
            useProgress: useProgress);

        Assert.Equal(0, results.Length);
    }

    [Theory, CombinatorialData]
    public async Task ReferenceSingleOtherDocument(bool mutatingLspWorkspace, bool useProgress)
    {
        var markup1 = """
            class X
            {
                Y y;
            }
            """;

        var markup2 = """
            class Y
            {
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync([markup1, markup2], mutatingLspWorkspace);

        var project = testLspServer.TestWorkspace.CurrentSolution.Projects.Single();
        var results = await RunGetRelatedDocumentsAsync(
            testLspServer,
            project.Documents.First().GetURI(),
            useProgress: useProgress);

        Assert.Equal(1, results.Length);
        Assert.Equal(project.Documents.Last().FilePath, results[0].FilePaths.Single());
    }

    [Theory, CombinatorialData]
    public async Task ReferenceMultipleOtherDocument(bool mutatingLspWorkspace, bool useProgress)
    {
        var markup1 = """
            class X
            {
                Y y;
                Z z;
            }
            """;

        var markup2 = """
            class Y
            {
            }
            """;

        var markup3 = """
            class Z
            {
            }
            """;

        await using var testLspServer = await CreateTestLspServerAsync([markup1, markup2, markup3], mutatingLspWorkspace);

        var project = testLspServer.TestWorkspace.CurrentSolution.Projects.Single();
        var results = await RunGetRelatedDocumentsAsync(
            testLspServer,
            project.Documents.First().GetURI(),
            useProgress: useProgress);

        Assert.Equal(1, results.Length);
        Assert.Equal(2, results[0].FilePaths!.Length);
        AssertEx.SetEqual([.. project.Documents.Skip(1).Select(d => d.FilePath)], results[0].FilePaths);
    }
}
