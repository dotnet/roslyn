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
                var span = annotatedSelection.SingleOrDefault();
                var cursorPosition = hostDocument.CursorPosition ?? span.Start;

                textView.Selection.Mode = TextSelectionMode.Stream;

                var snapshotSpan = new SnapshotSpan(subjectBuffer.CurrentSnapshot, span.Start, span.Length);
                var isReversed = cursorPosition == span.Start;

                textView.Selection.Select(snapshotSpan, isReversed);
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
