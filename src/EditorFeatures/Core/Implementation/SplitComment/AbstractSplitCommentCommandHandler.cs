// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
{
    internal abstract class AbstractSplitCommentCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        public abstract string DisplayName { get; }

        public abstract bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext executionContext);
        public abstract CommandState GetCommandState(ReturnKeyCommandArgs args);

        public abstract bool ExecuteCommandWorker(ReturnKeyCommandArgs args);

        protected abstract bool SplitComment(ITextView textView, ITextBuffer subjectBuffer, SnapshotPoint caret);
        protected abstract bool LineContainsComment(ITextSnapshotLine line, int caretPosition);
        protected abstract int? SplitComment(Document document, DocumentOptionSet options, int position, CancellationToken cancellationToken);
    }
}
