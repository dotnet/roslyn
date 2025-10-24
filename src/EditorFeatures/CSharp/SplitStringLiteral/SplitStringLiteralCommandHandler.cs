// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[Name(nameof(SplitStringLiteralCommandHandler))]
[Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class SplitStringLiteralCommandHandler(
    ITextUndoHistoryRegistry undoHistoryRegistry,
    IEditorOperationsFactoryService editorOperationsFactoryService,
    EditorOptionsService editorOptionsService) : ICommandHandler<ReturnKeyCommandArgs>
{
    private readonly ITextUndoHistoryRegistry _undoHistoryRegistry = undoHistoryRegistry;
    private readonly IEditorOperationsFactoryService _editorOperationsFactoryService = editorOperationsFactoryService;
    private readonly EditorOptionsService _editorOptionsService = editorOptionsService;

    public string DisplayName => CSharpEditorResources.Split_string;

    public CommandState GetCommandState(ReturnKeyCommandArgs args)
        => CommandState.Unspecified;

    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        => ExecuteCommandWorker(args, context.OperationContext.UserCancellationToken);

    public bool ExecuteCommandWorker(ReturnKeyCommandArgs args, CancellationToken cancellationToken)
    {
        if (!_editorOptionsService.GlobalOptions.GetOption(SplitStringLiteralOptionsStorage.Enabled))
        {
            return false;
        }

        var textView = args.TextView;
        var subjectBuffer = args.SubjectBuffer;
        var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

        // Don't split strings if there is any actual selection.
        // We must check all spans to account for multi-carets.
        if (spans.IsEmpty() || !spans.All(s => s.IsEmpty))
            return false;

        // Don't split strings if there are multiple carets.
        // We don't properly support multi-caret scenarios.
        if (spans.Count > 1)
            return false;

        var caret = textView.GetCaretPoint(subjectBuffer);
        if (caret == null)
            return false;

        // Verify that we are working with a string literal.
        // Otherwise, let the editor handle the caret.
        var span = spans[0];
        var line = subjectBuffer.CurrentSnapshot.GetLineFromPosition(span.Start);
        if (!LineContainsQuote(line, span.Start))
            return false;

        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return false;

        var parsedDocument = ParsedDocument.CreateSynchronously(document, CancellationToken.None);
        var indentationOptions = subjectBuffer.GetIndentationOptions(_editorOptionsService, document.Project.GetFallbackAnalyzerOptions(), parsedDocument.LanguageServices, explicitFormat: false);

        using var transaction = CaretPreservingEditTransaction.TryCreate(
            CSharpEditorResources.Split_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

        var splitter = StringSplitter.TryCreate(parsedDocument, span.Start.Position, indentationOptions, cancellationToken);
        if (splitter is null ||
            !splitter.TrySplit(out var newRoot, out var newPosition))
        {
            return false;
        }

        // apply the change:
        var newDocument = parsedDocument.WithChangedRoot(newRoot, cancellationToken);
        var newSnapshot = subjectBuffer.ApplyChanges(newDocument.GetChanges(parsedDocument));

        // Adjust the caret position.
        var newCaretPoint = textView.BufferGraph.MapUpToBuffer(
            new SnapshotPoint(newSnapshot, newPosition),
            PointTrackingMode.Negative,
            PositionAffinity.Predecessor,
            textView.TextBuffer);

        if (newCaretPoint != null)
            textView.Caret.MoveTo(newCaretPoint.Value);

        transaction?.Complete();

        return true;

        static bool LineContainsQuote(ITextSnapshotLine line, int caretPosition)
        {
            var snapshot = line.Snapshot;
            for (int i = line.Start; i < caretPosition; i++)
            {
                if (snapshot[i] == '"')
                    return true;
            }

            return false;
        }
    }
}
