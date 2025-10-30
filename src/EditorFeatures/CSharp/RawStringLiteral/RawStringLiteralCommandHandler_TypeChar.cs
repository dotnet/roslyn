// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.RawStringLiteral;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CSharp.RawStringLiteral;

internal partial class RawStringLiteralCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
{
    public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextCommandHandler)
        => nextCommandHandler();

    public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext context)
    {
        if (!ExecuteCommandWorker(args, nextCommandHandler))
            nextCommandHandler();
    }

    private bool ExecuteCommandWorker(TypeCharCommandArgs args, Action nextCommandHandler)
    {
        if (args.TypedChar != '"')
            return false;

        var textView = args.TextView;
        var subjectBuffer = args.SubjectBuffer;
        var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

        if (spans.Count != 1)
            return false;

        var span = spans.First();
        if (span.Length != 0)
            return false;

        var caret = textView.GetCaretPoint(subjectBuffer);
        if (caret == null)
            return false;

        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return false;

        // This command handler only runs for C#, which should always provide this service.
        var service = document.Project.GetRequiredLanguageService<IRawStringLiteralAutoInsertService>();

        var cancellationToken = CancellationToken.None;
        var text = document.GetTextSynchronously(cancellationToken);
        var textChangeOpt = service.GetTextChangeForQuote(document, text, caret.Value.Position, cancellationToken);

        if (textChangeOpt is not TextChange textChange)
            return false;

        // Looks good.  First, let the quote get added by the normal type char handlers.  Then make our text change.
        // We do this in two steps so that undo can work properly.
        nextCommandHandler();

        using var transaction = CaretPreservingEditTransaction.TryCreate(
            CSharpEditorResources.Grow_raw_string, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

        var edit = subjectBuffer.CreateEdit();
        edit.Insert(textChange.Span.Start, textChange.NewText);
        edit.Apply();

        // ensure the caret is placed after where the original quote got added.
        textView.Caret.MoveTo(new SnapshotPoint(subjectBuffer.CurrentSnapshot, caret.Value.Position + 1));

        transaction?.Complete();
        return true;
    }
}
