﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<WordDeleteToStartCommandArgs>,
        ICommandHandler<WordDeleteToEndCommandArgs>
    {
        public CommandState GetCommandState(WordDeleteToStartCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public CommandState GetCommandState(WordDeleteToEndCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(WordDeleteToStartCommandArgs args, Action nextHandler)
        {
            if (HandleWordDeleteCommand(args.SubjectBuffer, args.TextView, deleteToStart: true))
            {
                return;
            }

            nextHandler();
        }

        public void ExecuteCommand(WordDeleteToEndCommandArgs args, Action nextHandler)
        {
            if (HandleWordDeleteCommand(args.SubjectBuffer, args.TextView, deleteToStart: false))
            {
                return;
            }

            nextHandler();
        }

        private bool HandleWordDeleteCommand(ITextBuffer subjectBuffer, ITextView view, bool deleteToStart)
        {
            if (_renameService.ActiveSession == null)
            {
                return false;
            }

            var caretPoint = view.GetCaretPoint(subjectBuffer);
            if (caretPoint.HasValue)
            {
                if (_renameService.ActiveSession.TryGetContainingEditableSpan(caretPoint.Value, out var span))
                {
                    int start = caretPoint.Value;
                    int end = caretPoint.Value;
                    if (!view.Selection.IsEmpty)
                    {
                        var selectedSpans = view.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
                        if (selectedSpans.Count == 1 && span.Contains(selectedSpans.Single().Span))
                        {
                            // We might want to delete past the caret's active position if there's a selection
                            start = selectedSpans.Single().Start;
                            end = selectedSpans.Single().End;
                        }
                        else
                        {
                            // we're outside of an editable span, so let this command go to the next handler
                            return false;
                        }
                    }

                    subjectBuffer.Delete(deleteToStart
                        ? Span.FromBounds(span.Start, end)
                        : Span.FromBounds(start, span.End));

                    return true;
                }
            }

            return false;
        }
    }
}
