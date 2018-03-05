// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        internal override void OnTextViewBufferPostChanged(object sender, EventArgs e)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // No session, so we don't need to do anything.
                return;
            }

            // If we have a session active then it may be in the process of computing results.  If it
            // has computed the results, then compare where the caret is with all the items.  If the
            // caret isn't within the bounds of the items, then we dismiss completion. If they have
            // not computed results yet, then chain a task to see if we should dismiss the list.  
            var model = sessionOpt.Computation.InitialUnfilteredModel;
            if (model != null && this.IsCaretOutsideAllItemBounds(model, this.GetCaretPointInViewBuffer()))
            {
                // If the caret moved out of bounds of our items, then we want to dismiss the list. 
                this.DismissSessionIfActive();
            }
            else
            {
                // Something changed in the buffer without us hearing about the change first.
                // This can happen in complex projection scenarios (with buffers being mapped/unmapped).
                // In this case, queue up a CaretPositionChanged filter first.  That way if the caret
                // moved out of bounds of the items, then we'll dismiss.  Also queue up an insertion.
                // that way we go and filter things properly to ensure that the list contains the 
                // appropriate items.
                sessionOpt.FilterModel(CompletionFilterReason.CaretPositionChanged, filterState: null);
                sessionOpt.FilterModel(CompletionFilterReason.Insertion, filterState: null);
            }
        }
    }
}
