// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Metadata;

public sealed class LspMetadataAsSourceWorkspaceTests : AbstractLanguageServerProtocolTests
{
    public LspMetadataAsSourceWorkspaceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestMetadataFile_OpenClosed(bool mutatingLspWorkspace)
    {
        var source =
            """
            using System;
            class A
            {
                void M()
                {
                    Console.{|definition:WriteLine|}("Hello, World!");
                }
            }
            """;

        // Create a server with LSP misc file workspace and metadata service.
        await using var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Get the metadata definition.
        var location = testLspServer.GetLocations("definition").Single();
        var definition = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                           CreateTextDocumentPositionParams(location), CancellationToken.None);
        AssertEx.NotNull(definition);

        // Open the metadata file and verify it gets added to the metadata workspace.
        await testLspServer.OpenDocumentAsync(definition.Single().DocumentUri, text: string.Empty).ConfigureAwait(false);

        Assert.Equal(WorkspaceKind.MetadataAsSource, (await GetWorkspaceForDocument(testLspServer, definition.Single().DocumentUri)).Kind);
        await AssertMiscFileWorkspaceEmpty(testLspServer);

        // Close the metadata file - the file will still be present in MAS.
        await testLspServer.CloseDocumentAsync(definition.Single().DocumentUri).ConfigureAwait(false);

        Assert.Equal(WorkspaceKind.MetadataAsSource, (await GetWorkspaceForDocument(testLspServer, definition.Single().DocumentUri)).Kind);
        await AssertMiscFileWorkspaceEmpty(testLspServer);
    }

    [Theory, CombinatorialData]
    public async Task TestMetadataFile_LanguageFeatures(bool mutatingLspWorkspace)
    {
        var source =
            """
            using System;
            class A
            {
                void M()
                {
                    Console.{|definition:WriteLine|}("Hello, World!");
                }
            }
            """;

        // Create a server with LSP misc file workspace and metadata service.
        await using var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Get the metadata definition.
        var location = testLspServer.GetLocations("definition").Single();
        var definition = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                           CreateTextDocumentPositionParams(location), CancellationToken.None);
        AssertEx.NotNull(definition);

        // Open the metadata file and verify it gets added to the metadata workspace.
        // We don't have the real metadata source, so just populate it with our fake metadata source.
        await testLspServer.OpenDocumentAsync(definition.Single().DocumentUri, text: """
            namespace System
            {
                public class Console
                {
                    public static void WriteLine(string value) {}
                }
            }
            """).ConfigureAwait(false);
        var workspaceForDocument = await GetWorkspaceForDocument(testLspServer, definition.Single().DocumentUri);
        Assert.Equal(WorkspaceKind.MetadataAsSource, workspaceForDocument.Kind);
        await AssertMiscFileWorkspaceEmpty(testLspServer);

        // Manually register the workspace for followup requests - the workspace event listener that
        //  normally registers it on creation is not running in test code.
        testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LspWorkspaceRegistrationService>().Register(workspaceForDocument);

        var locationOfStringKeyword = new LSP.Location
        {
            DocumentUri = definition.Single().DocumentUri,
            Range = new LSP.Range
            {
                Start = new LSP.Position { Line = 4, Character = 40 },
                End = new LSP.Position { Line = 4, Character = 40 }
            }
        };

        var definitionFromMetadata = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                           CreateTextDocumentPositionParams(locationOfStringKeyword), CancellationToken.None);

        Assert.NotNull(definitionFromMetadata);
        Assert.NotEmpty(definitionFromMetadata);
        Assert.Contains("String.cs", definitionFromMetadata.Single().DocumentUri.UriString);
    }

    private static async Task<Workspace> GetWorkspaceForDocument(TestLspServer testLspServer, DocumentUri fileUri)
    {
        var (lspWorkspace, _, _) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = fileUri }, CancellationToken.None);
        return lspWorkspace!;
    }

    private static async Task AssertMiscFileWorkspaceEmpty(TestLspServer testLspServer)
    {
        var docs = await testLspServer.GetManagerAccessor().GetMiscellaneousDocumentsAsync(static p => p.Documents).ToImmutableArrayAsync(CancellationToken.None);
        Assert.Empty(docs);
    }
}
