// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;

public class HtmlCodeActionProviderTest(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
{
    [Fact]
    public async Task ProvideAsync_WrapsResolvableCodeActions()
    {
        // Arrange
        var contents = "<$$h1>Goo</h1>";
        TestFileMarkupParser.GetPosition(contents, out contents, out var cursorPosition);

        var documentPath = "c:/Test.razor";
        var request = new VSCodeActionParams()
        {
            TextDocument = new VSTextDocumentIdentifier { DocumentUri = new(new Uri(documentPath)) },
            Range = LspFactory.DefaultRange,
            Context = new VSInternalCodeActionContext()
        };

        var context = CreateRazorCodeActionContext(request, cursorPosition, documentPath, contents);

        var razorEditService = StrictMock.Of<IRazorEditService>();
        var provider = new HtmlCodeActionProvider(razorEditService);

        ImmutableArray<RazorVSInternalCodeAction> codeActions = [new RazorVSInternalCodeAction() { Name = "Test" }];

        // Act
        var providedCodeActions = await provider.ProvideAsync(context, codeActions, DisposalToken);

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

        var documentPath = "c:/Test.razor";
        var request = new VSCodeActionParams()
        {
            TextDocument = new VSTextDocumentIdentifier { DocumentUri = new(new Uri(documentPath)) },
            Range = LspFactory.DefaultRange,
            Context = new VSInternalCodeActionContext()
        };

        var context = CreateRazorCodeActionContext(request, cursorPosition, documentPath, contents);

        var razorEditServiceMock = new StrictMock<IRazorEditService>();
        razorEditServiceMock
            .Setup(x => x.MapWorkspaceEditAsync(It.IsAny<IDocumentSnapshot>(), It.IsAny<WorkspaceEdit>(), It.IsAny<CancellationToken>()))
            .Callback<IDocumentSnapshot, WorkspaceEdit, CancellationToken>((_, edit, _) =>
            {
                var textDocumentEdit = edit.EnumerateTextDocumentEdits().First();
                textDocumentEdit.TextDocument.DocumentUri = new(documentPath);
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
                                DocumentUri = new(new Uri("c:/Test.razor.html")),
                            },
                            Edits = [LspFactory.CreateTextEdit(position: (0, 0), "Goo")]
                        }
                    }
                }
            }
        ];

        // Act
        var providedCodeActions = await provider.ProvideAsync(context, codeActions, DisposalToken);

        // Assert
        var action = Assert.Single(providedCodeActions);
        Assert.NotNull(action.Edit);
        var documentEdits = action.Edit.EnumerateTextDocumentEdits().ToArray();
        Assert.NotEmpty(documentEdits);
        Assert.Equal(documentPath, documentEdits[0].TextDocument.DocumentUri.GetRequiredParsedUri().AbsolutePath);

        var text = SourceText.From(contents);
        var changed = text.WithChanges(documentEdits[0].Edits.Select(e => text.GetTextChange((TextEdit)e)));
        Assert.Equal("Goo @(DateTime.Now) Bar", changed.ToString());
    }

    private static RazorCodeActionContext CreateRazorCodeActionContext(
        VSCodeActionParams request,
        int absoluteIndex,
        string filePath,
        string text,
        bool supportsFileCreation = true,
        bool supportsCodeActionResolve = true)
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
        documentSnapshotMock
            .Setup(x => x.GetTextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(codeDocument.Source.Text);
        documentSnapshotMock
            .Setup(x => x.Project.GetTagHelpersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tagHelpers);

        return new RazorCodeActionContext(
            request,
            documentSnapshotMock.Object,
            codeDocument,
            DelegatedDocumentUri: null,
            StartAbsoluteIndex: absoluteIndex,
            EndAbsoluteIndex: absoluteIndex,
            RazorLanguageKind.Html,
            codeDocument.Source.Text,
            supportsFileCreation,
            supportsCodeActionResolve);

    }
}
