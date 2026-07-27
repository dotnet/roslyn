// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Moq;
using Roslyn.LanguageServer.Protocol;
using Xunit;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

public class HtmlCodeActionProviderTest
{
    [Fact]
    public async Task ProvideAsync_WrapsResolvableCodeActions()
    {
        // Arrange
        var contents = "<$$h1>Goo</h1>";
        TestFileMarkupParser.GetPosition(contents, out contents, out var cursorPosition);

        var documentPath = TestProjectData.SomeProjectComponentFile1.FilePath;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath);
        var request = new VSCodeActionParams()
        {
            TextDocument = new VSTextDocumentIdentifier { DocumentUri = documentUri },
            Range = LspFactory.DefaultRange,
            Context = new VSInternalCodeActionContext()
        };

        var (context, workspace) = await CreateRazorCodeActionContextAsync(request, cursorPosition, documentPath, contents);
        using var workspaceLifetime = workspace;

        var razorEditService = StrictMock.Of<IRazorEditService>();
        var provider = new HtmlCodeActionProvider(razorEditService);

        ImmutableArray<RazorVSInternalCodeAction> codeActions = [new RazorVSInternalCodeAction() { Name = "Test" }];

        // Act
        var providedCodeActions = await provider.ProvideAsync(context, codeActions, CancellationToken.None);

        // Assert
        var action = Assert.Single(providedCodeActions);
        Assert.Equal("Test", action.Name);
        Assert.Equal(RazorLanguageKind.Html, (RazorLanguageKind)((JsonElement)action.Data!).GetProperty("language").GetUInt32());
    }

    [Fact]
    public async Task ProvideAsync_RemapsAndFixesEdits()
    {
        // Arrange
        var contents = "[|<$$h1>Goo @(DateTime.Now) Bar</h1>|]";
        TestFileMarkupParser.GetPositionAndSpan(contents, out contents, out var cursorPosition, out var span);

        var documentPath = TestProjectData.SomeProjectComponentFile1.FilePath;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath);
        var request = new VSCodeActionParams()
        {
            TextDocument = new VSTextDocumentIdentifier { DocumentUri = documentUri },
            Range = LspFactory.DefaultRange,
            Context = new VSInternalCodeActionContext()
        };

        var (context, workspace) = await CreateRazorCodeActionContextAsync(request, cursorPosition, documentPath, contents);
        using var workspaceLifetime = workspace;

        var razorEditServiceMock = new StrictMock<IRazorEditService>();
        razorEditServiceMock
            .Setup(x => x.MapWorkspaceEditAsync(It.IsAny<IDocumentSnapshot>(), It.IsAny<WorkspaceEdit>(), It.IsAny<CancellationToken>()))
            .Callback<IDocumentSnapshot, WorkspaceEdit, CancellationToken>((snapshot, edit, _) =>
            {
                Assert.IsType<RemoteDocumentSnapshot>(snapshot);
                var textDocumentEdit = edit.EnumerateTextDocumentEdits().First();
                textDocumentEdit.TextDocument.DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath);
                textDocumentEdit.Edits = [LspFactory.CreateTextEdit(context.SourceText.GetRange(span), "Goo /*~~~~~~~~~~~*/ Bar")];
            })
            .Returns(Task.CompletedTask);

        var provider = new HtmlCodeActionProvider(razorEditServiceMock.Object);

        ImmutableArray<RazorVSInternalCodeAction> codeActions =
        [
            new RazorVSInternalCodeAction()
            {
                Name = "Test",
                Edit = new WorkspaceEdit
                {
                    DocumentChanges = new TextDocumentEdit[]
                    {
                        new() {
                            TextDocument = new OptionalVersionedTextDocumentIdentifier
                            {
                                DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath + ".html"),
                            },
                            Edits = [LspFactory.CreateTextEdit(position: (0, 0), "Goo")]
                        }
                    }
                }
            }
        ];

        // Act
        var providedCodeActions = await provider.ProvideAsync(context, codeActions, CancellationToken.None);

        // Assert
        var action = Assert.Single(providedCodeActions);
        Assert.NotNull(action.Edit);
        var documentEdits = action.Edit.EnumerateTextDocumentEdits().ToArray();
        Assert.NotEmpty(documentEdits);
        Assert.Equal(documentUri, documentEdits[0].TextDocument.DocumentUri);

        var text = SourceText.From(contents);
        var changed = text.WithChanges(documentEdits[0].Edits.Select(e => text.GetTextChange((TextEdit)e)));
        Assert.Equal("Goo @(DateTime.Now) Bar", changed.ToString());
    }

    private static async Task<(RazorCodeActionContext Context, AdhocWorkspace Workspace)> CreateRazorCodeActionContextAsync(
        VSCodeActionParams request,
        int absoluteIndex,
        string filePath,
        string text)
    {
        var sourceText = SourceText.From(text);
        var workspace = new AdhocWorkspace();
        var builder = new RazorProjectBuilder
        {
            ProjectFilePath = TestProjectData.SomeProject.FilePath,
        };
        builder.AddReferences(AspNet80.ReferenceInfos.All.Select(static r => r.Reference));
        var documentId = builder.AddAdditionalDocument(filePath, sourceText);
        var solution = builder.Build(workspace.CurrentSolution);

        workspace.TryApplyChanges(solution);
        var document = workspace.CurrentSolution.GetAdditionalDocument(documentId)!;
        var snapshotManager = new RemoteSnapshotManager(new RemoteFilePathService(), NoOpTelemetryReporter.Instance);
        var snapshot = snapshotManager.GetSnapshot(document);
        var codeDocument = await snapshot.GetGeneratedOutputAsync(CancellationToken.None).ConfigureAwait(false);

        var context = new RazorCodeActionContext(
            request,
            snapshot,
            codeDocument,
            DelegatedDocumentUri: null,
            StartAbsoluteIndex: absoluteIndex,
            EndAbsoluteIndex: absoluteIndex,
            RazorLanguageKind.Html,
            codeDocument.Source.Text,
            SupportsFileCreation: true,
            SupportsCodeActionResolve: true);

        return (context, workspace);
    }
}
