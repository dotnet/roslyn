// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Roslyn.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

public class HtmlCodeActionResolverTest
{
    [Fact]
    public async Task ResolveAsync_RemapsAndFixesEdits()
    {
        // Arrange
        var contents = "[|<$$h1>Goo @(DateTime.Now) Bar</h1>|]";
        TestFileMarkupParser.GetPositionAndSpan(contents, out contents, out _, out var span);

        var documentPath = "c:/Test.razor";
        var documentUri = new Uri(documentPath);
        var (context, sourceText) = CreateDocumentContext(documentUri, documentPath, contents);

        var razorEditServiceMock = new StrictMock<IRazorEditService>();
        razorEditServiceMock
            .Setup(x => x.MapWorkspaceEditAsync(It.IsAny<IDocumentSnapshot>(), It.IsAny<WorkspaceEdit>(), It.IsAny<CancellationToken>()))
            .Callback<IDocumentSnapshot, WorkspaceEdit, CancellationToken>((_, edit, _) =>
            {
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
                            DocumentUri = new(new Uri("c:/Test.razor.html")),
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
        Assert.Equal(documentPath, documentEdits[0].TextDocument.DocumentUri.GetRequiredParsedUri().AbsolutePath);

        var text = SourceText.From(contents);
        var changed = text.WithChanges(documentEdits[0].Edits.Select(e => text.GetTextChange((TextEdit)e)));
        Assert.Equal("Goo @(DateTime.Now) Bar", changed.ToString());
    }

    private static (DocumentContext Context, SourceText SourceText) CreateDocumentContext(Uri documentUri, string filePath, string text)
    {
        var tagHelpers = TagHelperCollection.Empty;
        var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.SetTagHelpers(tagHelpers);

            builder.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
            });
        });

        var codeDocument = projectEngine.Process(sourceDocument, RazorFileKind.Legacy, importSources: default, tagHelpers);
        var documentSnapshotMock = new StrictMock<IDocumentSnapshot>();
        documentSnapshotMock
            .Setup(x => x.GetGeneratedOutputAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeDocument);

        return (new DocumentContext(documentUri, documentSnapshotMock.Object), codeDocument.Source.Text);
    }
}
