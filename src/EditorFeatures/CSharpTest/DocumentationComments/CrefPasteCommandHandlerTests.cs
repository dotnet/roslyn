// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DocumentationComments;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.DocumentationComments)]
public sealed class CrefPasteCommandHandlerTests
{
    private static void TestPaste(string markup, string pasteText, string expectedMarkup, string? afterUndoMarkup = null)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(markup);
        var document = workspace.Documents.Single();
        var textView = document.GetTextView();
        var textBuffer = document.GetTextBuffer();

        if (document.SelectedSpans.Count > 0)
        {
            var selection = document.SelectedSpans.Single().ToSnapshotSpan(textBuffer.CurrentSnapshot);
            textView.Selection.Select(selection, isReversed: false);
            textView.Caret.MoveTo(selection.End);
        }

        var commandHandler = workspace.ExportProvider.GetCommandHandler<CrefPasteCommandHandler>(
            PredefinedCommandHandlerNames.CrefPaste, ContentTypeNames.CSharpContentType);
        var editorOperations = workspace.GetService<IEditorOperationsFactoryService>().GetEditorOperations(textView);

        commandHandler.ExecuteCommand(
            new PasteCommandArgs(textView, textBuffer),
            () => editorOperations.ReplaceSelection(pasteText),
            TestCommandExecutionContext.Create());

        MarkupTestFile.GetPosition(expectedMarkup, out var expected, out int expectedCaret);
        Assert.Equal(expected, textBuffer.CurrentSnapshot.GetText());
        Assert.Equal(expectedCaret, textView.Caret.Position.BufferPosition.Position);

        if (afterUndoMarkup != null)
        {
            workspace.GetService<ITextUndoHistoryRegistry>().GetHistory(textBuffer).Undo(1);

            MarkupTestFile.GetPosition(afterUndoMarkup, out var afterUndo, out int afterUndoCaret);
            Assert.Equal(afterUndo, textBuffer.CurrentSnapshot.GetText());
            Assert.Equal(afterUndoCaret, textView.Caret.Position.BufferPosition.Position);
        }
    }

    [WpfFact]
    public void PasteGenericTypeIntoEmptyCref()
        => TestPaste("""
            /// <see cref="$$"/>
            class C { }
            """, "List<int>", """
            /// <see cref="List{int}$$"/>
            class C { }
            """);

    [WpfFact]
    public void PasteNestedGenericTypeIntoCref()
        => TestPaste("""
            /// <see cref="$$"/>
            class C { }
            """, "Dictionary<string, List<int>>", """
            /// <see cref="Dictionary{string, List{int}}$$"/>
            class C { }
            """);

    [WpfFact]
    public void PasteGenericMethodIntoCref()
        => TestPaste("""
            /// <see cref="$$"/>
            class C { }
            """, "Enumerable.Select<TSource, TResult>(IEnumerable<TSource>, Func<TSource, TResult>)", """
            /// <see cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})$$"/>
            class C { }
            """);

    [WpfFact]
    public void PasteIntoMiddleOfCref()
        => TestPaste("""
            /// <see cref="System.Collections.Generic.$$"/>
            class C { }
            """, "List<T>", """
            /// <see cref="System.Collections.Generic.List{T}$$"/>
            class C { }
            """);

    [WpfFact]
    public void PasteOverSelectionInCref()
        => TestPaste("""
            /// <see cref="[|Goo|]"/>
            class C { }
            """, "List<T>", """
            /// <see cref="List{T}$$"/>
            class C { }
            """);

    [WpfFact]
    public void PasteIntoCrefWithinSummary()
        => TestPaste("""
            /// <summary>
            /// Uses <see cref="$$"/>.
            /// </summary>
            class C { }
            """, "IEnumerable<T>", """
            /// <summary>
            /// Uses <see cref="IEnumerable{T}$$"/>.
            /// </summary>
            class C { }
            """);

    [WpfFact]
    public void PasteIntoSingleQuotedCref()
        => TestPaste("""
            /// <see cref='$$'/>
            class C { }
            """, "List<int>", """
            /// <see cref='List{int}$$'/>
            class C { }
            """);

    [WpfFact]
    public void UndoRestoresOriginalPaste()
        => TestPaste("""
            /// <see cref="$$"/>
            class C { }
            """, "List<int>", """
            /// <see cref="List{int}$$"/>
            class C { }
            """, afterUndoMarkup: """
            /// <see cref="List<int>$$"/>
            class C { }
            """);

    [WpfFact]
    public void NoChangeWithoutGenericBrackets()
        => TestPaste("""
            /// <see cref="$$"/>
            class C { }
            """, "List{int}", """
            /// <see cref="List{int}$$"/>
            class C { }
            """);

    [WpfFact]
    public void NoChangeInDocCommentText()
        => TestPaste("""
            /// <summary>Returns $$</summary>
            class C { }
            """, "List<int>", """
            /// <summary>Returns List<int>$$</summary>
            class C { }
            """);

    [WpfFact]
    public void NoChangeInOtherAttribute()
        => TestPaste("""
            /// <param name="$$"/>
            class C { }
            """, "List<int>", """
            /// <param name="List<int>$$"/>
            class C { }
            """);

    [WpfFact]
    public void NoChangeBeforeCrefValue()
        => TestPaste("""
            /// <see $$cref="Goo"/>
            class C { }
            """, "List<int>", """
            /// <see List<int>$$cref="Goo"/>
            class C { }
            """);

    [WpfFact]
    public void NoChangeWhenPastedTextContainsQuote()
        => TestPaste("""
            /// <see cref="$$"/>
            class C { }
            """, "List<int>\"/> <see cref=\"Goo", """
            /// <see cref="List<int>"/> <see cref="Goo$$"/>
            class C { }
            """);

    [WpfFact]
    public void NoChangeInRegularCode()
        => TestPaste("""
            class C
            {
                $$
            }
            """, "List<int> x;", """
            class C
            {
                List<int> x;$$
            }
            """);

    [WpfFact]
    public void NoChangeInStringLiteral()
        => TestPaste("""
            class C
            {
                string s = "cref=\"$$\"";
            }
            """, "List<int>", """
            class C
            {
                string s = "cref=\"List<int>$$\"";
            }
            """);
}
