// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.StringCopyPaste;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste;

internal partial class StringCopyPasteCommandHandler :
    IChainedCommandHandler<CutCommandArgs>,
    IChainedCommandHandler<CopyCommandArgs>
{
    public const string KeyAndVersion = nameof(StringCopyPasteCommandHandler) + "V1";

    public CommandState GetCommandState(CutCommandArgs args, Func<CommandState> nextCommandHandler)
        => nextCommandHandler();

    public CommandState GetCommandState(CopyCommandArgs args, Func<CommandState> nextCommandHandler)
        => nextCommandHandler();

    public void ExecuteCommand(CutCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        => ExecuteCutOrCopyCommand(args.TextView, args.SubjectBuffer, nextCommandHandler, executionContext);

    public void ExecuteCommand(CopyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        => ExecuteCutOrCopyCommand(args.TextView, args.SubjectBuffer, nextCommandHandler, executionContext);

    private void ExecuteCutOrCopyCommand(ITextView textView, ITextBuffer subjectBuffer, Action nextCommandHandler, CommandExecutionContext executionContext)
    {
        Contract.ThrowIfFalse(_threadingContext.HasMainThread);
        var (dataToStore, copyPasteService) = CaptureCutCopyInformation(textView, subjectBuffer, executionContext.OperationContext.UserCancellationToken);

        // Ensure that the copy always goes through all other handlers.
        nextCommandHandler();

        // Always try to store our data to the clipboard (if we have access to the clipboard service).  Even if we
        // didn't capture any useful data, we want to store that to blow away any prior stored data we have.
        copyPasteService?.TrySetClipboardData(KeyAndVersion, dataToStore ?? "");
    }

    private static (string? dataToStore, IStringCopyPasteService service) CaptureCutCopyInformation(
        ITextView textView, ITextBuffer subjectBuffer, CancellationToken cancellationToken)
    {
        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return default;

        var copyPasteService = document.Project.Solution.Services.GetService<IStringCopyPasteService>();
        if (copyPasteService == null)
            return default;

        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);

        var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

        // We only support smart copy/paste when a single selection is copied (and a single selection is pasted
        // over).  This vastly simplifies the logic we need, and it means we don't have to try to reimplement the 
        // editor logic for what it means when you are copying X selections and pasting over Y selections.
        if (spans.Count != 1)
            return default;

        var span = spans[0];
        var snapshot = span.Snapshot;
        if (span.IsEmpty)
        {
            // cut/copy on an empty span means "cut/copy the entire line".
            var line = snapshot.GetLineFromPosition(span.Start);
            span = line.ExtentIncludingLineBreak;
        }

        var stringExpression = TryGetCompatibleContainingStringExpression(
            parsedDocument, new NormalizedSnapshotSpanCollection(span));
        if (stringExpression is null)
            return default;

        var virtualCharService = document.GetRequiredLanguageService<IVirtualCharLanguageService>();
        var stringData = StringCopyPasteData.TryCreate(virtualCharService, stringExpression, span.Span.ToTextSpan());

        return (stringData?.ToJson(), copyPasteService);
    }
}
