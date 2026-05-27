// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Moq;
using Roslyn.LanguageServer.Protocol;
using Xunit;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

public class HtmlCodeActionResolverTest
{
    [Fact]
    public async Task ResolveAsync_RemapsAndFixesEdits()
    {
        // Arrange
        var contents = "[|<$$h1>Goo @(DateTime.Now) Bar</h1>|]";
        TestFileMarkupParser.GetPositionAndSpan(contents, out contents, out _, out var span);

        var documentPath = TestProjectData.SomeProjectComponentFile1.FilePath;
        var documentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath);
        var (context, sourceText, workspace) = CreateDocumentContext(documentUri, documentPath, contents);
        using var workspaceLifetime = workspace;

        var razorEditServiceMock = new StrictMock<IRazorEditService>();
        razorEditServiceMock
            .Setup(x => x.MapWorkspaceEditAsync(It.IsAny<IDocumentSnapshot>(), It.IsAny<WorkspaceEdit>(), It.IsAny<CancellationToken>()))
            .Callback<IDocumentSnapshot, WorkspaceEdit, CancellationToken>((snapshot, edit, _) =>
            {
                Assert.IsType<RemoteDocumentSnapshot>(snapshot);
                var textDocumentEdit = edit.EnumerateTextDocumentEdits().First();
                textDocumentEdit.TextDocument.DocumentUri = new(documentPath);
                textDocumentEdit.Edits = [LspFactory.CreateTextEdit(sourceText.GetRange(span), "Goo /*~~~~~~~~~~~*/ Bar")];
            })
            .Returns(Task.CompletedTask);

        var resolver = new HtmlCodeActionResolver(razorEditServiceMock.Object);

        var codeAction = new RazorVSInternalCodeAction()
        {
            Name = "Test",
            Edit = new WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new()
                    {
                        TextDocument = new OptionalVersionedTextDocumentIdentifier
                        {
                            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath + ".html"),
                        },
                        Edits = [LspFactory.CreateTextEdit(position: (0, 0), "Goo")]
                    }
                }
            }
        };

        // Act
        var action = await resolver.ResolveAsync(context, codeAction, CancellationToken.None);

        // Assert
        Assert.NotNull(action.Edit);
        var documentEdits = action.Edit.EnumerateTextDocumentEdits().ToArray();
        Assert.NotEmpty(documentEdits);
        Assert.Equal(documentUri, documentEdits[0].TextDocument.DocumentUri);

        var text = SourceText.From(contents);
        var changed = text.WithChanges(documentEdits[0].Edits.Select(e => text.GetTextChange((TextEdit)e)));
        Assert.Equal("Goo @(DateTime.Now) Bar", changed.ToString());
    }

    private static (RemoteDocumentContext Context, SourceText SourceText, AdhocWorkspace Workspace) CreateDocumentContext(DocumentUri documentUri, string filePath, string text)
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

        return (new RemoteDocumentContext(documentUri, snapshot), sourceText, workspace);
    }
}
