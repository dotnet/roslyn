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
                this.StopModelComputation();
            }
            else
            {
                // Filter the model, recheck the caret position if we haven't computed the initial model yet
                sessionOpt.FilterModel(
                    CompletionFilterReason.TypeChar,
                    recheckCaretPosition: model == null,
                    dismissIfEmptyAllowed: true,
                    filterState: null);
            }
        }
    }
}