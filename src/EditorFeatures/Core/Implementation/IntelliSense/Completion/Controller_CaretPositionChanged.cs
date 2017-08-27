// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal override void OnCaretPositionChanged(object sender, EventArgs args)
        {
            AssertIsForeground();

            if (!IsSessionActive)
            {
                // No session, so we don't need to do anything.
                return;
            }

            // If we have a session active then it may be in the process of computing results.  If it
            // has computed the results, then compare where the caret is with all the items.  If the
            // caret isn't within the bounds of the items, then we dismiss completion.
            var caretPoint = this.GetCaretPointInViewBuffer();
            var model = sessionOpt.Computation.InitialUnfilteredModel;
            if (model == null)
            {
                // Completions hadn't even been computed yet. Just cancel everything we're doing
                // and move to the Inactive state.
                this.DismissSessionIfActive();
                return;
            }

            // We're currently computing items. We'll need to make sure that the caret point
            // hasn't moved outside all of the items.  If so, we'd want to dismiss completions.
            // Just refilter the list, asking it to make sure that the caret is still within
            // bounds.
            sessionOpt.FilterModel(CompletionFilterReason.CaretPositionChanged, filterState: null);
        }

        internal bool IsCaretOutsideAllItemBounds(Model model, SnapshotPoint caretPoint)
        {
            var textSpanToTextCache = new Dictionary<TextSpan, string>();
            var textSpanToViewSpanCache = new Dictionary<TextSpan, ViewTextSpan>();

            foreach (var item in model.TotalItems)
            {
                if (!IsCaretOutsideItemBounds(model, caretPoint, item, textSpanToTextCache, textSpanToViewSpanCache))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsCaretOutsideItemBounds(
            Model model,
            SnapshotPoint caretPoint,
            CompletionItem item,
            Dictionary<TextSpan, string> textSpanToText,
            Dictionary<TextSpan, ViewTextSpan> textSpanToViewSpan)
        {
            // Easy first check.  See if the caret point is before the start of the item.
            if (!textSpanToViewSpan.TryGetValue(item.Span, out var filterSpanInViewBuffer))
            {
                filterSpanInViewBuffer = model.GetViewBufferSpan(item.Span);
                textSpanToViewSpan[item.Span] = filterSpanInViewBuffer;
            }

            if (caretPoint < filterSpanInViewBuffer.TextSpan.Start)
            {
                return true;
            }

            var textSnapshot = caretPoint.Snapshot;

            var currentText = model.GetCurrentTextInSnapshot(item.Span, textSnapshot, textSpanToText);
            var currentTextSpan = new TextSpan(filterSpanInViewBuffer.TextSpan.Start, currentText.Length);

            return !currentTextSpan.IntersectsWith(caretPoint);
        }
    }
}
