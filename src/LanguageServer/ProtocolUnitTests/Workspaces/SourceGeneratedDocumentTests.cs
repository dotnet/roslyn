// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SourceGenerators;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;

public sealed class SourceGeneratedDocumentTests(ITestOutputHelper? testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task ReturnsTextForSourceGeneratedDocument(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, "// Hello, World");

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);

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
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);

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
                new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);
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

        var location = new LSP.Location { DocumentUri = sourceGeneratorDocumentUri, Range = new LSP.Range { Start = new LSP.Position(0, 6), End = new LSP.Position(0, 6) } };

        var hover = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName,
                CreateTextDocumentPositionParams(location), CancellationToken.None);

        AssertEx.NotNull(hover);
        Assert.Contains("class A", hover.Contents.Fourth.Value);
    }

    [Theory, CombinatorialData]
    public async Task ReturnsGeneratedSourceForOpenDocument(bool mutatingLspWorkspace)
    {
        var sourceGeneratorSource = "// Hello, World";
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, sourceGeneratorSource);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        // Open the document with different text - this will cause the queue to generate frozen sg documents using this value.
        // However the get text handler should return the real source generator source.
        await testLspServer.OpenDocumentAsync(sourceGeneratorDocumentUri, "LSP Open Document Text");

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);

        AssertEx.NotNull(text);
        Assert.Equal(sourceGeneratorSource, text.Text);
    }

    [Theory, CombinatorialData]
    public async Task TestReturnsUnchangedResult(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, "// Hello, World");

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);

        AssertEx.NotNull(text);
        Assert.Equal("// Hello, World", text.Text);

        // Make a second request - since nothing has changed we should get back the same resultId.
        var secondRequest = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: text.ResultId), CancellationToken.None);
        AssertEx.NotNull(secondRequest);
        Assert.Null(secondRequest.Text);
        Assert.Equal(text.ResultId, secondRequest.ResultId);
    }

    [Theory, CombinatorialData]
    internal async Task TestReturnsGeneratedSourceWhenDocumentChanges(bool mutatingLspWorkspace, SourceGeneratorExecutionPreference sourceGeneratorExecution)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);

        var configService = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<TestWorkspaceConfigurationService>();
        configService.Options = new WorkspaceConfigurationOptions(SourceGeneratorExecution: sourceGeneratorExecution);

        var callCount = 0;
        var generatorReference = await AddGeneratorAsync(new CallbackGenerator(() => ("hintName.cs", "// callCount: " + callCount++)), testLspServer.TestWorkspace);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);

        AssertEx.NotNull(text);
        Assert.Equal("// callCount: 0", text.Text);

        // Modify a normal document in the workspace.
        // In automatic mode this should trigger generators to re-run.
        // In balanced mode generators should not re-run.
        await testLspServer.TestWorkspace.ChangeDocumentAsync(testLspServer.TestWorkspace.Documents.Single(d => !d.IsSourceGenerated).Id, SourceText.From("new text"));
        await testLspServer.WaitForSourceGeneratorsAsync();

        // Ask for the source generated text again.
        var secondRequest = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: text.ResultId), CancellationToken.None);

        if (sourceGeneratorExecution == SourceGeneratorExecutionPreference.Automatic)
        {
            // We should get newly generated text
            AssertEx.NotNull(secondRequest);
            Assert.NotEqual(text.ResultId, secondRequest.ResultId);
            Assert.Equal("// callCount: 1", secondRequest.Text);
        }
        else
        {
            // We should get an unchanged result
            AssertEx.NotNull(secondRequest);
            Assert.Equal(text.ResultId, secondRequest.ResultId);
            Assert.Null(secondRequest.Text);
        }
    }

    [Theory, CombinatorialData]
    internal async Task TestReturnsGeneratedSourceWhenManuallyRefreshed(bool mutatingLspWorkspace, SourceGeneratorExecutionPreference sourceGeneratorExecution)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);

        var configService = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<TestWorkspaceConfigurationService>();
        configService.Options = new WorkspaceConfigurationOptions(SourceGeneratorExecution: sourceGeneratorExecution);

        var callCount = 0;
        var generatorReference = await AddGeneratorAsync(new CallbackGenerator(() => ("hintName.cs", "// callCount: " + callCount++)), testLspServer.TestWorkspace);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);

        AssertEx.NotNull(text);
        Assert.Equal("// callCount: 0", text.Text);

        // Updating the execution version should trigger source generators to run in both automatic and balanced mode.
        await testLspServer.RefreshSourceGeneratorsAsync(forceRegeneration: true);

        var secondRequest = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: text.ResultId), CancellationToken.None);
        AssertEx.NotNull(secondRequest);
        Assert.NotEqual(text.ResultId, secondRequest.ResultId);
        Assert.Equal("// callCount: 1", secondRequest.Text);
    }

    [Theory, CombinatorialData]
    internal async Task TestCanRunSourceGeneratorAndApplyChangesConcurrently(
        bool mutatingLspWorkspace,
        bool majorVersionUpdate,
        SourceGeneratorExecutionPreference sourceGeneratorExecution)
    {
        await using var testLspServer = await CreateTestLspServerAsync("""
            class C
            {
            }
            """, mutatingLspWorkspace);

        var configService = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<TestWorkspaceConfigurationService>();
        configService.Options = new WorkspaceConfigurationOptions(SourceGeneratorExecution: sourceGeneratorExecution);

        var callCount = 0;
        var generatorReference = await AddGeneratorAsync(new CallbackGenerator(() => ("hintName.cs", "// callCount: " + callCount++)), testLspServer.TestWorkspace);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);

        AssertEx.NotNull(text);
        Assert.Equal("// callCount: 0", text.Text);

        var initialSolution = testLspServer.GetCurrentSolution();
        var initialExecutionMap = initialSolution.CompilationState.SourceGeneratorExecutionVersionMap.Map;

        // Updating the execution version should trigger source generators to run if there are any changes in both automatic and balanced mode.
        var forceRegeneration = majorVersionUpdate;
        await testLspServer.RefreshSourceGeneratorsAsync(forceRegeneration);

        var solutionWithChangedExecutionVersion = testLspServer.GetCurrentSolution();

        var secondRequest = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: text.ResultId), CancellationToken.None);
        AssertEx.NotNull(secondRequest);

        if (forceRegeneration)
        {
            Assert.NotEqual(text.ResultId, secondRequest.ResultId);
            Assert.Equal("// callCount: 1", secondRequest.Text);
        }
        else
        {
            // There are no changes, so source generators won't actually run if we didn't force regeneration
            Assert.Equal(text.ResultId, secondRequest.ResultId);
            Assert.Null(secondRequest.Text);
        }

        var projectId1 = initialSolution.ProjectIds.Single();
        var solutionWithDocumentChanged = initialSolution.WithDocumentText(
            initialSolution.Projects.Single().Documents.Single().Id,
            SourceText.From("class D { }"));

        var expectVersionChange = sourceGeneratorExecution is SourceGeneratorExecutionPreference.Balanced || forceRegeneration;

        // The content forked solution should have an SG execution version *less than* the one we just changed.
        // Note: this will be patched up once we call TryApplyChanges.
        if (expectVersionChange)
        {
            Assert.True(
                solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]
                > solutionWithDocumentChanged.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
        }
        else
        {
            Assert.Equal(
                solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1],
                solutionWithDocumentChanged.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
        }

        Assert.True(testLspServer.TestWorkspace.TryApplyChanges(solutionWithDocumentChanged));

        var finalSolution = testLspServer.GetCurrentSolution();

        if (expectVersionChange)
        {
            // In balanced (or if we forced regen) mode, the execution version should have been updated to the new value.
            Assert.NotEqual(initialExecutionMap[projectId1], solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
            Assert.NotEqual(initialExecutionMap[projectId1], finalSolution.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
        }
        else
        {
            // In automatic mode, nothing should change wrt to execution versions (unless we specified force-regenerate).
            Assert.Equal(initialExecutionMap[projectId1], solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
            Assert.Equal(initialExecutionMap[projectId1], finalSolution.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
        }

        // The final execution version for the project should match the changed execution version, no matter what.
        // Proving that the content change happened, but didn't drop the execution version change.
        Assert.Equal(solutionWithChangedExecutionVersion.CompilationState.SourceGeneratorExecutionVersionMap[projectId1], finalSolution.CompilationState.SourceGeneratorExecutionVersionMap[projectId1]);
    }

    [Theory, CombinatorialData]
    public async Task TestReturnsNullForRemovedClosedGeneratedFile(bool mutatingLspWorkspace)
    {
        var generatorText = "// Hello, World";
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);
        var generatorReference = await AddGeneratorAsync(new SingleFileTestGenerator(generatorText), testLspServer.TestWorkspace);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);
        AssertEx.NotNull(text);
        Assert.Equal("// Hello, World", text.Text);

        // Remove the generator and verify that we get null text back.
        await RemoveGeneratorAsync(generatorReference, testLspServer.TestWorkspace);

        var secondRequest = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: text.ResultId), CancellationToken.None);

        Assert.NotNull(secondRequest);
        Assert.Null(secondRequest.Text);
    }

    [Theory, CombinatorialData]
    public async Task TestReturnsNullForRemovedOpenedGeneratedFile(bool mutatingLspWorkspace)
    {
        var generatorText = "// Hello, World";
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);
        var generatorReference = await AddGeneratorAsync(new SingleFileTestGenerator(generatorText), testLspServer.TestWorkspace);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);
        AssertEx.NotNull(text);
        Assert.Equal("// Hello, World", text.Text);

        // Open the document - this will cause the queue to generate frozen sg documents based on the LSP open text
        // even if the source generator is removed entirely.
        await testLspServer.OpenDocumentAsync(sourceGeneratorDocumentUri, text.Text);

        // Remove the generator - the handler should return null text.
        await RemoveGeneratorAsync(generatorReference, testLspServer.TestWorkspace);

        var secondRequest = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: text.ResultId), CancellationToken.None);

        Assert.NotNull(secondRequest);
        Assert.Null(secondRequest.Text);
    }

    [Theory, CombinatorialData]
    internal async Task TestSaveRefreshesSourceGenerators(bool mutatingLspWorkspace, SourceGeneratorExecutionPreference sourceGeneratorExecution)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);
        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
        var documentUri = document.GetURI();

        var configService = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<TestWorkspaceConfigurationService>();
        configService.Options = new WorkspaceConfigurationOptions(SourceGeneratorExecution: sourceGeneratorExecution);

        var callCount = 0;
        var generatorReference = await AddGeneratorAsync(new CallbackGenerator(() => ("hintName.cs", "// callCount: " + callCount++)), testLspServer.TestWorkspace);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var text = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: null), CancellationToken.None);

        AssertEx.NotNull(text);
        Assert.Equal("// callCount: 0", text.Text);

        await testLspServer.OpenDocumentAsync(documentUri, string.Empty);

        // Modify a normal document in the workspace.
        // In automatic mode this should trigger generators to re-run.
        // In balanced mode generators should not re-run.
        await testLspServer.TestWorkspace.ChangeDocumentAsync(document.Id, SourceText.From("new text"));
        await testLspServer.WaitForSourceGeneratorsAsync();

        var secondRequest = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: text.ResultId), CancellationToken.None);
        AssertEx.NotNull(secondRequest);
        if (sourceGeneratorExecution == SourceGeneratorExecutionPreference.Automatic)
        {
            Assert.NotEqual(text.ResultId, secondRequest.ResultId);
            Assert.Equal("// callCount: 1", secondRequest.Text);
        }
        else
        {
            Assert.Equal(text.ResultId, secondRequest.ResultId);
        }

        var didSaveParams = new LSP.DidSaveTextDocumentParams
        {
            TextDocument = new LSP.TextDocumentIdentifier { DocumentUri = documentUri },
        };

        // The didSave should now trigger generators to run in balanced mode.  In automatic mode it will also trigger but we will already have the updated text.
        await testLspServer.ExecuteRequestAsync<LSP.DidSaveTextDocumentParams, object>(LSP.Methods.TextDocumentDidSaveName, didSaveParams, CancellationToken.None);
        await testLspServer.WaitForSourceGeneratorsAsync();

        var thirdRequest = await testLspServer.ExecuteRequestAsync<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>(SourceGeneratedDocumentGetTextHandler.MethodName,
            new SourceGeneratorGetTextParams(new LSP.TextDocumentIdentifier { DocumentUri = sourceGeneratorDocumentUri }, ResultId: text.ResultId), CancellationToken.None);
        AssertEx.NotNull(thirdRequest);
        Assert.NotEqual(secondRequest.ResultId, thirdRequest.ResultId);
        Assert.Equal("// callCount: 1", thirdRequest.Text);
    }

    private async Task<TestLspServer> CreateTestLspServerWithGeneratorAsync(
        bool mutatingLspWorkspace,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string generatedDocumentText)
    {
        var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);
        await AddGeneratorAsync(new SingleFileTestGenerator(generatedDocumentText), testLspServer.TestWorkspace);
        return testLspServer;
    }
}
