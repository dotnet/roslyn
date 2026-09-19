// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.LanguageServer;

public sealed class AlwaysActivateInProcCapabilitiesProviderTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    protected override TestComposition Composition => EditorTestCompositions.LanguageServerProtocolEditorFeatures
        .AddParts(typeof(TestDocumentTrackingService))
        .AddParts(typeof(TestLspLoggerFactory));

    [Theory, CombinatorialData]
    public async Task TextlessDidOpenCapabilityIsNegotiated(bool mutatingLspWorkspace, bool supportsOmittingText)
    {
        var clientCapabilities = new VSInternalClientCapabilities
        {
            SupportsVisualStudioExtensions = true,
            SupportsNotIncludingTextInTextDocumentDidOpen = supportsOmittingText,
        };

        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace, clientCapabilities);
        var serverCapabilities = Assert.IsType<VSInternalServerCapabilities>(server.GetServerCapabilities());

        Assert.Equal(supportsOmittingText, serverCapabilities.DoNotIncludeTextInTextDocumentDidOpen);
    }

    [Theory, CombinatorialData]
    public async Task TextlessDidOpenUsesWorkspaceText(bool mutatingLspWorkspace)
    {
        var clientCapabilities = new VSInternalClientCapabilities
        {
            SupportsVisualStudioExtensions = true,
            SupportsNotIncludingTextInTextDocumentDidOpen = true,
        };

        await using var server = await CreateTestLspServerAsync("""
            class A
            {
                {|document:|}
            }
            """, mutatingLspWorkspace, clientCapabilities);

        var workspaceDocument = server.GetCurrentSolution().Projects.Single().Documents.Single();
        var workspaceText = SourceText.From("class B { }");
        await server.OpenDocumentInWorkspaceAsync(workspaceDocument.Id, openAllLinkedDocuments: false, workspaceText);

        var didOpenParams = new LSP.DidOpenTextDocumentParams
        {
            TextDocument = new LSP.TextDocumentItem
            {
                DocumentUri = server.GetLocations("document").Single().DocumentUri,
                LanguageId = LanguageNames.CSharp,
                Text = null,
            },
        };

        await server.ExecuteRequestAsync<LSP.DidOpenTextDocumentParams, object>(
            LSP.Methods.TextDocumentDidOpenName, didOpenParams, CancellationToken.None);

        Assert.Same(workspaceText, server.GetTrackedTexts().Single());
    }
}
