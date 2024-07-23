// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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

        // Open the metadata file and verify it gets added to the metadata workspace.
        await testLspServer.OpenDocumentAsync(definition.Single().Uri, text: string.Empty).ConfigureAwait(false);

        Assert.Equal(WorkspaceKind.MetadataAsSource, (await GetWorkspaceForDocument(testLspServer, definition.Single().Uri)).Kind);
        AssertMiscFileWorkspaceEmpty(testLspServer);

        // Close the metadata file and verify it gets removed from the metadata workspace.
        await testLspServer.CloseDocumentAsync(definition.Single().Uri).ConfigureAwait(false);

        AssertMetadataFileWorkspaceEmpty(testLspServer);
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

        var metadataSource =
            """
            namespace System
            {
                public class Console
                {
                    public static void WriteLine(string value) {}
                }
            }
            """;

        // Create a server with LSP misc file workspace and metadata service.
        await using var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Get the metadata definition.
        var location = testLspServer.GetLocations("definition").Single();
        var definition = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                           CreateTextDocumentPositionParams(location), CancellationToken.None);

        // Open the metadata file and verify it gets added to the metadata workspace.
        // We don't have the real metadata source, so just populate it with our fake metadata source.
        await testLspServer.OpenDocumentAsync(definition.Single().Uri, text: metadataSource).ConfigureAwait(false);
        var workspaceForDocument = await GetWorkspaceForDocument(testLspServer, definition.Single().Uri);
        Assert.Equal(WorkspaceKind.MetadataAsSource, workspaceForDocument.Kind);
        AssertMiscFileWorkspaceEmpty(testLspServer);

        // Manually register the workspace for followup requests - the workspace event listener that
        //  normally registers it on creation is not running in test code.
        testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LspWorkspaceRegistrationService>().Register(workspaceForDocument);

        var locationOfStringKeyword = new LSP.Location
        {
            Uri = definition.Single().Uri,
            Range = new LSP.Range
            {
                Start = new LSP.Position { Line = 4, Character = 40 },
                End = new LSP.Position { Line = 4, Character = 40 }
            }
        };

        var definitionFromMetadata = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                           CreateTextDocumentPositionParams(locationOfStringKeyword), CancellationToken.None);

        Assert.NotEmpty(definitionFromMetadata);
        Assert.Contains("String.cs", definitionFromMetadata.Single().Uri.LocalPath);
    }

    private static async Task<Workspace> GetWorkspaceForDocument(TestLspServer testLspServer, Uri fileUri)
    {
        var (lspWorkspace, _, _) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { Uri = fileUri }, CancellationToken.None);
        return lspWorkspace!;
    }

    private static void AssertMiscFileWorkspaceEmpty(TestLspServer testLspServer)
    {
        var doc = testLspServer.GetManagerAccessor().GetLspMiscellaneousFilesWorkspace()!.CurrentSolution.Projects.SingleOrDefault()?.Documents.SingleOrDefault();
        Assert.Null(doc);
    }

    private static void AssertMetadataFileWorkspaceEmpty(TestLspServer testLspServer)
    {
        var provider = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IMetadataAsSourceFileService>();
        var metadataDocument = provider.TryGetWorkspace()?.CurrentSolution.Projects.SingleOrDefault()?.Documents.SingleOrDefault();
        Assert.Null(metadataDocument);
    }
}
