// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using StreamJsonRpc;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;

public sealed class SourceGeneratedDocumentTests(ITestOutputHelper? testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    private static readonly ClientCapabilities CapabilitiesWithRefresh = new()
    {
        Workspace = new WorkspaceClientCapabilities
        {
            TextDocumentContent = new TextDocumentContentClientCapabilities()
        }
    };

    [Theory, CombinatorialData]
    public async Task ReturnsTextForSourceGeneratedDocument(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, "// Hello, World");

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        AssertEx.NotNull(result);
        Assert.Equal("// Hello, World", result.Text);
    }

    [Theory, CombinatorialData]
    public async Task OpenCloseSourceGeneratedDocument(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, "// Hello, World");

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        AssertEx.NotNull(result);
        Assert.Equal("// Hello, World", result.Text);

        // Verifying opening and closing the document doesn't cause any issues.
        await testLspServer.OpenDocumentAsync(sourceGeneratorDocumentUri, result.Text);
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
            var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
            AssertEx.NotNull(result?.Text);
            await testLspServer.OpenDocumentAsync(sourceGeneratorDocumentUri, result.Text);
        }

        foreach (var sourceGeneratorDocumentUri in sourceGeneratorDocumentUris)
        {
            await testLspServer.CloseDocumentAsync(sourceGeneratorDocumentUri);
        }
    }

    [Theory, CombinatorialData]
    public async Task SetsTextDocumentContentCapabilitiesWhenSupported(bool mutatingLspWorkspace)
    {
        var clientCapabilities = new LSP.ClientCapabilities
        {
            Workspace = new LSP.WorkspaceClientCapabilities
            {
                TextDocumentContent = new LSP.TextDocumentContentClientCapabilities(),
            },
        };

        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, clientCapabilities);

        var textDocumentContentCapabilities = testLspServer.GetServerCapabilities().Workspace?.TextDocumentContent;
        AssertEx.NotNull(textDocumentContentCapabilities);
        Assert.Contains(SourceGeneratedDocumentUri.Scheme, textDocumentContentCapabilities.Schemes);
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

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        AssertEx.NotNull(result);
        Assert.Equal(sourceGeneratorSource, result.Text);
    }

    [Theory, CombinatorialData]
    public async Task TestReturnsUnchangedResult(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerWithGeneratorAsync(mutatingLspWorkspace, "// Hello, World");

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        AssertEx.NotNull(result);
        Assert.Equal("// Hello, World", result.Text);

        // Make a second request - since nothing has changed we should get back the same text.
        var secondResult = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        AssertEx.NotNull(secondResult);
        Assert.Equal("// Hello, World", secondResult.Text);
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

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        AssertEx.NotNull(result);
        Assert.Equal("// callCount: 0", result.Text);

        // Modify a normal document in the workspace.
        // In automatic mode this should trigger generators to re-run.
        // In balanced mode generators should not re-run.
        await testLspServer.TestWorkspace.ChangeDocumentAsync(testLspServer.TestWorkspace.Documents.Single(d => !d.IsSourceGenerated).Id, SourceText.From("new text"));
        await testLspServer.WaitForSourceGeneratorsAsync();

        // Ask for the source generated text again.
        var secondResult = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        if (sourceGeneratorExecution == SourceGeneratorExecutionPreference.Automatic)
        {
            // We should get newly generated text
            AssertEx.NotNull(secondResult);
            Assert.Equal("// callCount: 1", secondResult.Text);
        }
        else
        {
            // We should get the same text as before
            AssertEx.NotNull(secondResult);
            Assert.Equal("// callCount: 0", secondResult.Text);
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

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        AssertEx.NotNull(result);
        Assert.Equal("// callCount: 0", result.Text);

        // Updating the execution version should trigger source generators to run in both automatic and balanced mode.
        await testLspServer.RefreshSourceGeneratorsAsync(forceRegeneration: true);

        var secondResult = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        AssertEx.NotNull(secondResult);
        Assert.Equal("// callCount: 1", secondResult.Text);
    }

    [Theory, CombinatorialData]
    internal async Task TestTextDocumentContentRefreshSentWhenManuallyRefreshed(bool mutatingLspWorkspace, SourceGeneratorExecutionPreference sourceGeneratorExecution)
    {
        var clientCallbackTarget = new TextDocumentContentRefreshClientCallbackTarget();
        await using var testLspServer = await CreateTestLspServerAsync(
            string.Empty,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ClientTarget = clientCallbackTarget,
                ClientCapabilities = CapabilitiesWithRefresh
            });

        var configService = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<TestWorkspaceConfigurationService>();
        configService.Options = new WorkspaceConfigurationOptions(SourceGeneratorExecution: sourceGeneratorExecution);

        var callCount = 0;
        var generatorReference = await AddGeneratorAsync(new CallbackGenerator(() => ("hintName.cs", "// callCount: " + callCount++)), testLspServer.TestWorkspace);

        var sourceGeneratorDocumentUri = await OpenSingleSourceGeneratedDocumentAsync(testLspServer);
        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        clientCallbackTarget.Clear();

        await testLspServer.RefreshSourceGeneratorsAsync(forceRegeneration: true);

        Assert.Equal([sourceGeneratorDocumentUri], clientCallbackTarget.GetRefreshedUris());

        var refreshedResult = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        AssertEx.NotNull(refreshedResult);
        Assert.Equal("// callCount: 1", refreshedResult.Text);
    }

    [Theory, CombinatorialData]
    internal async Task TestTextDocumentContentRefreshSentWhenDocumentChanges(bool mutatingLspWorkspace, SourceGeneratorExecutionPreference sourceGeneratorExecution)
    {
        var clientCallbackTarget = new TextDocumentContentRefreshClientCallbackTarget();
        await using var testLspServer = await CreateTestLspServerAsync(
            string.Empty,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ClientTarget = clientCallbackTarget,
                ClientCapabilities = CapabilitiesWithRefresh
            });

        var configService = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<TestWorkspaceConfigurationService>();
        configService.Options = new WorkspaceConfigurationOptions(SourceGeneratorExecution: sourceGeneratorExecution);

        var callCount = 0;
        var generatorReference = await AddGeneratorAsync(new CallbackGenerator(() => ("hintName.cs", "// callCount: " + callCount++)), testLspServer.TestWorkspace);

        var sourceGeneratorDocumentUri = await OpenSingleSourceGeneratedDocumentAsync(testLspServer);
        clientCallbackTarget.Clear();

        await testLspServer.TestWorkspace.ChangeDocumentAsync(testLspServer.TestWorkspace.Documents.Single(d => !d.IsSourceGenerated).Id, SourceText.From("new text"));
        await testLspServer.WaitForSourceGeneratorsAsync();

        if (sourceGeneratorExecution == SourceGeneratorExecutionPreference.Automatic)
        {
            Assert.Equal([sourceGeneratorDocumentUri], clientCallbackTarget.GetRefreshedUris());
        }
        else
        {
            Assert.Empty(clientCallbackTarget.GetRefreshedUris());
        }

        var refreshedResult = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        AssertEx.NotNull(refreshedResult);
        Assert.Equal(
            sourceGeneratorExecution == SourceGeneratorExecutionPreference.Automatic ? "// callCount: 1" : "// callCount: 0",
            refreshedResult.Text);
    }

    [Theory, CombinatorialData]
    internal async Task TestTextDocumentContentRefreshSentWhenProjectChanges(bool mutatingLspWorkspace, SourceGeneratorExecutionPreference sourceGeneratorExecution)
    {
        var clientCallbackTarget = new TextDocumentContentRefreshClientCallbackTarget();
        await using var testLspServer = await CreateTestLspServerAsync(
            string.Empty,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ClientTarget = clientCallbackTarget,
                ClientCapabilities = CapabilitiesWithRefresh
            });

        var configService = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<TestWorkspaceConfigurationService>();
        configService.Options = new WorkspaceConfigurationOptions(SourceGeneratorExecution: sourceGeneratorExecution);

        var generatorReference = await AddGeneratorAsync(new SingleFileTestGenerator("// Hello, World"), testLspServer.TestWorkspace);

        var sourceGeneratorDocumentUri = await OpenSingleSourceGeneratedDocumentAsync(testLspServer);
        clientCallbackTarget.Clear();

        var project = testLspServer.TestWorkspace.CurrentSolution.Projects.Single();
        var updatedProject = project.WithAssemblyName(project.AssemblyName + "2");
        await testLspServer.TestWorkspace.ChangeProjectAsync(updatedProject.Id, updatedProject.Solution);
        await testLspServer.WaitForSourceGeneratorsAsync();

        Assert.Equal([sourceGeneratorDocumentUri], clientCallbackTarget.GetRefreshedUris());
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

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        AssertEx.NotNull(result);
        Assert.Equal("// callCount: 0", result.Text);

        var initialSolution = testLspServer.GetCurrentSolution();
        var initialExecutionMap = initialSolution.CompilationState.SourceGeneratorExecutionVersionMap.Map;

        // Updating the execution version should trigger source generators to run if there are any changes in both automatic and balanced mode.
        var forceRegeneration = majorVersionUpdate;
        await testLspServer.RefreshSourceGeneratorsAsync(forceRegeneration);

        var solutionWithChangedExecutionVersion = testLspServer.GetCurrentSolution();

        var secondResult = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        AssertEx.NotNull(secondResult);

        if (forceRegeneration)
        {
            Assert.Equal("// callCount: 1", secondResult.Text);
        }
        else
        {
            // There are no changes, so source generators won't actually run if we didn't force regeneration
            Assert.Equal("// callCount: 0", secondResult.Text);
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
    public async Task TestReturnsErrorForRemovedClosedGeneratedFile(bool mutatingLspWorkspace)
    {
        var generatorText = "// Hello, World";
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);
        var generatorReference = await AddGeneratorAsync(new SingleFileTestGenerator(generatorText), testLspServer.TestWorkspace);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        AssertEx.NotNull(result);
        Assert.Equal("// Hello, World", result.Text);

        // Remove the generator - the document is not opened so it will no longer exist in the workspace.
        // The handler should return an error since the document cannot be found.
        await RemoveGeneratorAsync(generatorReference, testLspServer.TestWorkspace);

        var exception = await Assert.ThrowsAsync<StreamJsonRpc.RemoteInvocationException>(
            () => testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Theory, CombinatorialData]
    public async Task TestReturnsEmptyForRemovedOpenedGeneratedFile(bool mutatingLspWorkspace)
    {
        var generatorText = "// Hello, World";
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);
        var generatorReference = await AddGeneratorAsync(new SingleFileTestGenerator(generatorText), testLspServer.TestWorkspace);

        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentIdentity = sourceGeneratedDocuments.Single().Identity;
        var sourceGeneratorDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocumentIdentity);

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        AssertEx.NotNull(result);
        Assert.Equal("// Hello, World", result.Text);

        // Open the document - this will cause the queue to generate frozen sg documents based on the LSP open text
        // even if the source generator is removed entirely.
        await testLspServer.OpenDocumentAsync(sourceGeneratorDocumentUri, result.Text);

        // Remove the generator - the handler should return empty text since the unfrozen document no longer exists.
        await RemoveGeneratorAsync(generatorReference, testLspServer.TestWorkspace);

        var secondResult = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        Assert.NotNull(secondResult);
        Assert.Empty(secondResult.Text);
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

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);

        AssertEx.NotNull(result);
        Assert.Equal("// callCount: 0", result.Text);

        await testLspServer.OpenDocumentAsync(documentUri, string.Empty);

        // Modify a normal document in the workspace.
        // In automatic mode this should trigger generators to re-run.
        // In balanced mode generators should not re-run.
        await testLspServer.TestWorkspace.ChangeDocumentAsync(document.Id, SourceText.From("new text"));
        await testLspServer.WaitForSourceGeneratorsAsync();

        var secondResult = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        AssertEx.NotNull(secondResult);
        if (sourceGeneratorExecution == SourceGeneratorExecutionPreference.Automatic)
        {
            Assert.Equal("// callCount: 1", secondResult.Text);
        }
        else
        {
            Assert.Equal("// callCount: 0", secondResult.Text);
        }

        var didSaveParams = new LSP.DidSaveTextDocumentParams
        {
            TextDocument = new LSP.TextDocumentIdentifier { DocumentUri = documentUri },
        };

        // The didSave should now trigger generators to run in balanced mode.  In automatic mode it will also trigger but we will already have the updated text.
        await testLspServer.ExecuteRequestAsync<LSP.DidSaveTextDocumentParams, object>(LSP.Methods.TextDocumentDidSaveName, didSaveParams, CancellationToken.None);
        await testLspServer.WaitForSourceGeneratorsAsync();

        var thirdResult = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratorDocumentUri);
        AssertEx.NotNull(thirdResult);
        Assert.Equal("// callCount: 1", thirdResult.Text);
    }

    private async Task<TestLspServer> CreateTestLspServerWithGeneratorAsync(
        bool mutatingLspWorkspace,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string generatedDocumentText)
    {
        var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);
        await AddGeneratorAsync(new SingleFileTestGenerator(generatedDocumentText), testLspServer.TestWorkspace);
        return testLspServer;
    }

    private static async Task<LSP.DocumentUri> OpenSingleSourceGeneratedDocumentAsync(TestLspServer testLspServer)
    {
        var sourceGeneratedDocuments = await testLspServer.GetCurrentSolution().Projects.Single().GetSourceGeneratedDocumentsAsync();
        var sourceGeneratedDocumentUri = SourceGeneratedDocumentUri.Create(sourceGeneratedDocuments.Single().Identity);

        var result = await testLspServer.GetSourceGeneratedDocumentTextAsync(sourceGeneratedDocumentUri);
        AssertEx.NotNull(result);

        await testLspServer.OpenDocumentAsync(sourceGeneratedDocumentUri, result.Text);
        await testLspServer.WaitForSourceGeneratorsAsync();
        return sourceGeneratedDocumentUri;
    }

    private sealed class TextDocumentContentRefreshClientCallbackTarget
    {
        private ConcurrentQueue<LSP.DocumentUri> _refreshedUris = new();

        [JsonRpcMethod(LSP.Methods.WorkspaceTextDocumentContentRefreshName, UseSingleObjectParameterDeserialization = true)]
        public object? WorkspaceTextDocumentContentRefresh(LSP.TextDocumentContentRefreshParams refreshParams, CancellationToken _)
        {
            _refreshedUris.Enqueue(refreshParams.Uri);
            return null;
        }

        public void Clear()
            => _refreshedUris = new ConcurrentQueue<LSP.DocumentUri>();

        public LSP.DocumentUri[] GetRefreshedUris()
            => [.. _refreshedUris];
    }
}
