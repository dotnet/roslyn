// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;

public class SourceGeneratedDocumentTests(ITestOutputHelper? testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task ReturnsTextForSourceGeneratedDocument(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, "// Hello, World");

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { Uri = sourceGeneratorDocumentUri }), CancellationToken.None);

        AssertEx.NotNull(text);
        Assert.Equal("// Hello, World", text.Text);
    }

    [Theory, CombinatorialData]
    public async Task OpenCloseSourceGeneratedDocument(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, "// Hello, World");

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { Uri = sourceGeneratorDocumentUri }), CancellationToken.None);

        AssertEx.NotNull(text);
        Assert.Equal("// Hello, World", text.Text);

        // Verifying opening and closing the document doesn't cause any issues.
        await testLspServer.OpenDocumentAsync(sourceGeneratorDocumentUri, text.Text);
        await testLspServer.CloseDocumentAsync(sourceGeneratorDocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task OpenMultipleSourceGeneratedDocument(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, "// Hello, World");

        await AddGeneratorAsync(new SingleFileTestGenerator2("// Goodbye"), testLspServer.TestWorkspace);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratorDocumentUris = sourceGeneratedDocuments.Select(s => SourceGeneratedDocumentUri.Create(s.Identity));

        Assert.Equal(2, sourceGeneratorDocumentUris.Count());

        foreach (var sourceGeneratorDocumentUri in sourceGeneratorDocumentUris)
        {
            var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
                new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { Uri = sourceGeneratorDocumentUri }), CancellationToken.None);
            AssertEx.NotNull(text?.Text);
            await testLspServer.OpenDocumentAsync(sourceGeneratorDocumentUri, text.Text);
        }

        foreach (var sourceGeneratorDocumentUri in sourceGeneratorDocumentUris)
        {
            await testLspServer.CloseDocumentAsync(sourceGeneratorDocumentUri);
        }
    }

    [Theory, CombinatorialData]
    public async Task RequestOnSourceGeneratedDocument(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, "class A { }");

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var location = new LSP.Location { Uri = sourceGeneratorDocumentUri, Range = new LSP.Range { Start = new LSP.Position(0, 6), End = new LSP.Position(0, 6) } };

        var hover = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName,
                CreateTextDocumentPositionParams(location), CancellationToken.None);

        AssertEx.NotNull(hover);
        Assert.Contains("class A", hover.Contents.Fourth.Value);
    }

    private async Task<TestLspServer> CreateTestLspServerWithGeneratorAsync(bool mutatingLspWorkspace, string generatedDocumentText)
    {
        var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);
        await AddGeneratorAsync(new SingleFileTestGenerator(generatedDocumentText), testLspServer.TestWorkspace);
        return testLspServer;
    }
}
