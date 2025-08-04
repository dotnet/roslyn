// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;
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
    public async Task TestMetadataFile_OpenClosed(bool mutatingLspWorkspace, bool useVirtualFiles)
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
        await using var testLspServer = await CreateLspServerWithMetadataAsSource(mutatingLspWorkspace, useVirtualFiles, source);

        // Get the metadata definition.
        var location = testLspServer.GetLocations("definition").Single();
        var (metadataPosition, text) = await CreateAndGetMetadataDocument(testLspServer, location, useVirtualFiles);

        // Open the metadata file and verify it gets added to the metadata workspace.
        await testLspServer.OpenDocumentAsync(metadataPosition.DocumentUri, text).ConfigureAwait(false);
        await VerifyDocumentInMetadataWorkspace(testLspServer, metadataPosition.DocumentUri);

        // Close the metadata file - the file will still be present in MAS.
        await testLspServer.CloseDocumentAsync(metadataPosition.DocumentUri).ConfigureAwait(false);
        await VerifyDocumentInMetadataWorkspace(testLspServer, metadataPosition.DocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task TestMetadataFile_LanguageFeatures(bool mutatingLspWorkspace, bool useVirtualFiles)
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

        await using var testLspServer = await CreateLspServerWithMetadataAsSource(mutatingLspWorkspace, useVirtualFiles, source);

        // Get the metadata definition.
        var location = testLspServer.GetLocations("definition").Single();
        var (metadataPosition, text) = await CreateAndGetMetadataDocument(testLspServer, location, useVirtualFiles);

        // Open the metadata file and verify it gets added to the metadata workspace.
        // We don't have the real metadata source, so just populate it with our fake metadata source.
        await testLspServer.OpenDocumentAsync(metadataPosition.DocumentUri, text).ConfigureAwait(false);
        await VerifyDocumentInMetadataWorkspace(testLspServer, metadataPosition.DocumentUri);

        var positionOfStringKeyword = new LSP.TextDocumentPositionParams()
        {
            TextDocument = CreateTextDocumentIdentifier(metadataPosition.DocumentUri),
            // The definition is at the start of "WriteLine(string value);".  The first location inside the string keyword is 11 characters in.
            Position = new LSP.Position(metadataPosition.Range.Start.Line, metadataPosition.Range.Start.Character + 11)
        };

        var definitionFromMetadata = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
            positionOfStringKeyword, CancellationToken.None);

        Assert.NotNull(definitionFromMetadata);
        Assert.NotEmpty(definitionFromMetadata);
        Assert.Contains("String.cs", definitionFromMetadata.Single().DocumentUri.UriString);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/7532")]
    public async Task TestMetadataFile_OpenDifferentCasing(bool mutatingLspWorkspace, bool useVirtualFiles)
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
        await using var testLspServer = await CreateLspServerWithMetadataAsSource(mutatingLspWorkspace, useVirtualFiles, source);

        // Get the metadata definition.
        var location = testLspServer.GetLocations("definition").Single();
        var (metadataPosition, text) = await CreateAndGetMetadataDocument(testLspServer, location, useVirtualFiles);

        var lowercaseDocumentUri = new DocumentUri(metadataPosition.DocumentUri.UriString.ToLowerInvariant());

        // Open the metadata file and verify it gets added to the metadata workspace.
        await testLspServer.OpenDocumentAsync(lowercaseDocumentUri, text).ConfigureAwait(false);
        await VerifyDocumentInMetadataWorkspace(testLspServer, lowercaseDocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task TestMetadataFile_GeneratesSameFile(bool mutatingLspWorkspace, bool useVirtualFiles)
    {
        var source =
            """
            using System;
            class A
            {
                void M()
                {
                    Console.{|firstCall:WriteLine|}("Hello, World!");
                    Console.{|secondCall:Write|}("Hello, World!");
                }
            }
            """;

        await using var testLspServer = await CreateLspServerWithMetadataAsSource(mutatingLspWorkspace, useVirtualFiles, source);

        // Get the first metadata definition.
        var firstLocation = testLspServer.GetLocations("firstCall").Single();
        var (firstMetadataPosition, firstDocumentText) = await CreateAndGetMetadataDocument(testLspServer, firstLocation, useVirtualFiles);

        // Get the second metadata definition.
        var secondLocation = testLspServer.GetLocations("secondCall").Single();
        var (secondMetadataPosition, secondDocumentText) = await CreateAndGetMetadataDocument(testLspServer, secondLocation, useVirtualFiles);

        // They should point to the same document, but different positions in it.
        Assert.Equal(firstMetadataPosition.DocumentUri, secondMetadataPosition.DocumentUri);
        Assert.NotEqual(firstMetadataPosition.Range, secondMetadataPosition.Range);
    }

    private async Task<TestLspServer> CreateLspServerWithMetadataAsSource(bool mutatingLspWorkspace, bool useVirtualFiles, string source)
    {
        var clientCapabilities = new LSP.ClientCapabilities
        {
            Workspace = new LSP.WorkspaceClientCapabilities
            {
                TextDocumentContent = useVirtualFiles ? new LSP.TextDocumentContentClientCapabilities() : null,
            }
        };
        var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace, new InitializationOptions
        {
            ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer,
            ClientCapabilities = clientCapabilities,
        });
        return testLspServer;
    }

    private static async Task VerifyDocumentInMetadataWorkspace(TestLspServer testLspServer, DocumentUri fileUri)
    {
        var (lspWorkspace, _, _) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = fileUri }, CancellationToken.None);

        Assert.Equal(WorkspaceKind.MetadataAsSource, lspWorkspace!.Kind);

        // Verify that the document is also not present in misc files.
        var doc = testLspServer.GetManagerAccessor().GetLspMiscellaneousFilesWorkspace()!.CurrentSolution.Projects.SingleOrDefault()?.Documents.SingleOrDefault();
        Assert.Null(doc);
    }

    private static async Task<(LSP.Location Position, string Text)> CreateAndGetMetadataDocument(TestLspServer testLspServer, LSP.Location requestLocation, bool useVirtualFiles)
    {
        var definition = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                           CreateTextDocumentPositionParams(requestLocation), CancellationToken.None);
        AssertEx.NotNull(definition);

        var metadataUri = definition.Single().DocumentUri;
        var text = await GetAndVerifyMetadataDocumentAsync(testLspServer, metadataUri, useVirtualFiles);
        return (definition.Single(), text);
    }

    private static async Task<string> GetAndVerifyMetadataDocumentAsync(TestLspServer testLspServer, DocumentUri documentUri, bool useVirtualFiles)
    {
        var parsedUri = documentUri.GetRequiredParsedUri();
        if (useVirtualFiles)
        {
            Assert.Equal(VirtualMetadataDocumentPersister.VirtualFileScheme, parsedUri.Scheme);
            Assert.False(File.Exists(parsedUri.LocalPath), "Virtual file should not exist on disk.");

            var result = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentContentParams, LSP.TextDocumentContentResult>(
                LSP.Methods.WorkspaceTextDocumentContentName,
                new LSP.TextDocumentContentParams { Uri = documentUri },
                CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(result);
            Assert.NotEmpty(result.Text);
            return result.Text;
        }
        else
        {
            Assert.NotEqual(VirtualMetadataDocumentPersister.VirtualFileScheme, parsedUri.Scheme);
            Assert.True(File.Exists(parsedUri.LocalPath));

            var text = File.ReadAllText(parsedUri.LocalPath);
            return text;
        }
    }
}
