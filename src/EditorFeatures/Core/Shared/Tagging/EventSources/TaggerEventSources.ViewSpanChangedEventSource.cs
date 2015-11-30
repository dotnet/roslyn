// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal partial class TaggerEventSources
    {
        private class ViewSpanChangedEventSource : ITaggerEventSource
        {
            private readonly ForegroundThreadAffinitizedObject _foregroundObject = new ForegroundThreadAffinitizedObject();

            private readonly ITextView _textView;
            private readonly TaggerDelay _textChangeDelay;
            private readonly TaggerDelay _scrollChangeDelay;

            private Span? _span;
            private ITextSnapshot _viewTextSnapshot;
            private ITextSnapshot _viewVisualSnapshot;

            public event EventHandler<TaggerEventArgs> Changed;
            public event EventHandler UIUpdatesPaused { add { } remove { } }
            public event EventHandler UIUpdatesResumed { add { } remove { } }

            public ViewSpanChangedEventSource(ITextView textView, TaggerDelay textChangeDelay, TaggerDelay scrollChangeDelay)
            {
                Debug.Assert(textView != null);
                _textView = textView;
                _textChangeDelay = textChangeDelay;
                _scrollChangeDelay = scrollChangeDelay;
            }

            public void Connect()
            {
                _foregroundObject.AssertIsForeground();
                _textView.LayoutChanged += OnLayoutChanged;
            }

            public void Disconnect()
            {
                _foregroundObject.AssertIsForeground();
                _textView.LayoutChanged -= OnLayoutChanged;
            }

            private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
            {
                _foregroundObject.AssertIsForeground();
                // The formatted span refers to the span of the textview's buffer that is visible.
                // If it changes, then we want to reclassify.  Note: the span might not change if
                // text were overwritten.  However, in the case of text-edits, we'll hear about 
                // through other means as we have an EventSource for that purpose.  This event
                // source is for knowing if the user moves the view around.  This handles direct
                // moves using the caret/scrollbar, as well as moves that happen because someone
                // jumped directly to a location using goto-def.  It also handles view changes
                // caused by the user collapsing an outlining region.

                var lastSpan = _span;
                var lastViewTextSnapshot = _viewTextSnapshot;
                var lastViewVisualSnapshot = _viewVisualSnapshot;

                _span = _textView.TextViewLines.FormattedSpan.Span;
                _viewTextSnapshot = _textView.TextSnapshot;
                _viewVisualSnapshot = _textView.VisualSnapshot;

                if (_span != lastSpan)
                {
                    // The span changed.  This could have happened for a few different reasons.  
                    // If none of the view's text snapshots changed, then it was because of scrolling.

                    if (_viewTextSnapshot == lastViewTextSnapshot &&
                        _viewVisualSnapshot == lastViewVisualSnapshot)
                    {
                        // We scrolled.
                        RaiseChanged(_scrollChangeDelay);
                    }
                    else
                    {
                        // text changed in some way.
                        RaiseChanged(_textChangeDelay);
                    }
                }
            }

            private void RaiseChanged(TaggerDelay delay)
            {
                this.Changed?.Invoke(this, new TaggerEventArgs(delay));
            }
        }
    }
}