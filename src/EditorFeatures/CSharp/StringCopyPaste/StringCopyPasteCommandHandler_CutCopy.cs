// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.StringCopyPaste;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.StringCopyPaste
{
    internal partial class StringCopyPasteCommandHandler :
        IChainedCommandHandler<CutCommandArgs>,
        IChainedCommandHandler<CopyCommandArgs>
    {
        public CommandState GetCommandState(CutCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public CommandState GetCommandState(CopyCommandArgs args, Func<CommandState> nextCommandHandler)
            => nextCommandHandler();

        public void ExecuteCommand(CutCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
            => ExecuteCommand(args.TextView, args.SubjectBuffer, nextCommandHandler);

        public void ExecuteCommand(CopyCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
            => ExecuteCommand(args.TextView, args.SubjectBuffer, nextCommandHandler);

        private void ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, Action nextCommandHandler)
        {
            Contract.ThrowIfFalse(_threadingContext.HasMainThread);
            CaptureCutCopyInformation(textView, subjectBuffer);

            // Ensure that the copy always goes through all other handlers.
            nextCommandHandler();
        }

        private void CaptureCutCopyInformation(ITextView textView, ITextBuffer subjectBuffer)
        {
            _lastClipboardSequenceNumber = null;
            _lastSelectedSpans = null;

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var copyPasteService = document?.Project.Solution.Workspace.Services.GetService<IStringCopyPasteService>();
            if (copyPasteService == null)
                return;

            var nextSequenceNumber = Interlocked.Increment(ref s_sequenceNumber);
            if (!copyPasteService.TrySetClipboardSequenceNumber(nextSequenceNumber))
                return;

            _lastClipboardSequenceNumber = nextSequenceNumber;
            _lastSelectedSpans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
        }
    }
}
