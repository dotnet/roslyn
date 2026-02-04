// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Outlining;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.GoToAdjacentMember;

[UseExportProvider]
public abstract class AbstractGoToAdjacentMemberTests
{
    protected abstract string LanguageName { get; }
    protected abstract ParseOptions DefaultParseOptions { get; }

    protected async Task AssertNavigatedAsync(string code, bool next, SourceCodeKind? sourceCodeKind = null)
    {
        var kinds = sourceCodeKind != null
            ? SpecializedCollections.SingletonEnumerable(sourceCodeKind.Value)
            : [SourceCodeKind.Regular, SourceCodeKind.Script];

        foreach (var kind in kinds)
        {
            using var workspace = EditorTestWorkspace.Create(
                LanguageName,
                compilationOptions: null,
                parseOptions: DefaultParseOptions.WithKind(kind),
                content: code);

            var hostDocument = workspace.DocumentWithCursor;
            var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
            var parsedDocument = await ParsedDocument.CreateAsync(document, CancellationToken.None);
            Assert.Empty(parsedDocument.SyntaxTree.GetDiagnostics());

            var textView = hostDocument.GetTextView();
            var subjectBuffer = hostDocument.GetTextBuffer();

            if (hostDocument.AnnotatedSpans.TryGetValue("selection", out var annotatedSelection) && annotatedSelection.Any())
            {
                var firstSpan = annotatedSelection.First();
                var lastSpan = annotatedSelection.Last();
                var cursorPosition = hostDocument.CursorPosition ?? firstSpan.Start;

                textView.Selection.Mode = annotatedSelection.Length > 1
                    ? TextSelectionMode.Box
                    : TextSelectionMode.Stream;

                SnapshotPoint boxSelectionStart, boxSelectionEnd;
                bool isReversed;

                if (cursorPosition == firstSpan.Start || cursorPosition == lastSpan.End)
                {
                    boxSelectionStart = new SnapshotPoint(subjectBuffer.CurrentSnapshot, firstSpan.Start);
                    boxSelectionEnd = new SnapshotPoint(subjectBuffer.CurrentSnapshot, lastSpan.End);
                    isReversed = cursorPosition == firstSpan.Start;
                }
                else
                {
                    boxSelectionStart = new SnapshotPoint(subjectBuffer.CurrentSnapshot, firstSpan.End);
                    boxSelectionEnd = new SnapshotPoint(subjectBuffer.CurrentSnapshot, lastSpan.Start);
                    isReversed = cursorPosition == firstSpan.End;
                }

                textView.Selection.Select(new SnapshotSpan(boxSelectionStart, boxSelectionEnd), isReversed);
            }
            else
            {
                textView.Caret.MoveTo(new SnapshotPoint(subjectBuffer.CurrentSnapshot, hostDocument.CursorPosition.Value));
            }

            var handler = workspace.ExportProvider.GetCommandHandler<GoToAdjacentMemberCommandHandler>(PredefinedCommandHandlerNames.GoToAdjacentMember, ContentTypeNames.RoslynContentType);

            EditorCommandArgs args = next
                ? new GoToNextMemberCommandArgs(textView, subjectBuffer)
                : new GoToPreviousMemberCommandArgs(textView, subjectBuffer);

            var executed = next
                ? handler.ExecuteCommand((GoToNextMemberCommandArgs)args, TestCommandExecutionContext.Create())
                : handler.ExecuteCommand((GoToPreviousMemberCommandArgs)args, TestCommandExecutionContext.Create());

            Assert.True(executed, "Command handler should have executed.");

            var finalPosition = textView.Caret.Position.BufferPosition.Position;

            Assert.Equal(hostDocument.SelectedSpans.Single().Start, finalPosition);

            if (hostDocument.AnnotatedSpans.ContainsKey("selection") && hostDocument.AnnotatedSpans["selection"].Any())
            {
                var selectionSpan = hostDocument.AnnotatedSpans["selection"].Single();
                Assert.True(selectionSpan.Length > 0, "Selection span should have length > 0");
            }
        }
    }

    protected async Task<int?> GetTargetPositionAsync(string code, bool next)
    {
        using var workspace = TestWorkspace.Create(
            LanguageName,
            compilationOptions: null,
            parseOptions: DefaultParseOptions,
            content: code);
        var hostDocument = workspace.DocumentWithCursor;
        var document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
        var parsedDocument = await ParsedDocument.CreateAsync(document, CancellationToken.None);
        Assert.Empty(parsedDocument.SyntaxTree.GetDiagnostics());

        return GoToAdjacentMemberCommandHandler.GetTargetPosition(
            document.GetRequiredLanguageService<ISyntaxFactsService>(),
            parsedDocument.Root,
            hostDocument.CursorPosition.Value,
            next);
    }
}
