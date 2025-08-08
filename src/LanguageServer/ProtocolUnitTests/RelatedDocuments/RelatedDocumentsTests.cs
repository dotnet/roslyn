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
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RelatedDocuments;

public sealed class RelatedDocumentsTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    private static async Task<VSInternalRelatedDocumentReport[]> RunGetRelatedDocumentsAsync(
        TestLspServer testLspServer,
        DocumentUri uri,
        bool useProgress = false)
    {
        BufferedProgress<VSInternalRelatedDocumentReport[]>? progress = useProgress ? BufferedProgress.Create<VSInternalRelatedDocumentReport[]>(null) : null;
        var spans = await testLspServer.ExecuteRequestAsync<VSInternalRelatedDocumentParams, VSInternalRelatedDocumentReport[]>(
            VSInternalMethods.CopilotRelatedDocumentsName,
            new VSInternalRelatedDocumentParams
            {
                TextDocument = new TextDocumentIdentifier { DocumentUri = uri },
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

        Assert.Empty(results);
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
        Assert.Equal(project.Documents.Last().FilePath, results[0].FilePaths!.Single());
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

        Assert.Equal(2, results.SelectMany(r => r.FilePaths!).Count());
        AssertEx.SetEqual([.. project.Documents.Skip(1).Select(d => d.FilePath)], results.SelectMany(r => r.FilePaths!));
    }

    [Theory, CombinatorialData]
    public async Task TestRepeatInvocations(bool mutatingLspWorkspace, bool useProgress)
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
        var results1 = await RunGetRelatedDocumentsAsync(
            testLspServer,
            project.Documents.First().GetURI(),
            useProgress: useProgress);

        var expectedResult = new VSInternalRelatedDocumentReport[]
        {
            new()
            {
                FilePaths = [project.Documents.Last().FilePath!],
            }
        };

        AssertJsonEquals(results1, expectedResult);

        // Calling again, without a change, should return the old result id and no filepaths.
        var results2 = await RunGetRelatedDocumentsAsync(
            testLspServer,
            project.Documents.First().GetURI(),
            useProgress: useProgress);

        AssertJsonEquals(results2, expectedResult);
    }

    [Theory, CombinatorialData]
    public async Task DoesNotIncludeSourceGeneratedDocuments(bool mutatingLspWorkspace, bool useProgress)
    {
        var source =
            """
            namespace M
            {
                class A
                {
                    public {|caret:|}B b;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        await AddGeneratorAsync(new SingleFileTestGenerator("""
            namespace M
            {
                class B
                {
                }
            }
            """), testLspServer.TestWorkspace);

        var project = testLspServer.TestWorkspace.CurrentSolution.Projects.Single();
        var results = await RunGetRelatedDocumentsAsync(
            testLspServer,
            project.Documents.First().GetURI(),
            useProgress: useProgress);

        Assert.Empty(results);
    }
}
