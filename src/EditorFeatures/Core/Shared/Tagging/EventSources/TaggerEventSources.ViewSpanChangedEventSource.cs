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
            private readonly ForegroundThreadAffinitizedObject foregroundObject = new ForegroundThreadAffinitizedObject();

            private readonly ITextView textView;
            private readonly TaggerDelay textChangeDelay;
            private readonly TaggerDelay scrollChangeDelay;

            private Span? span;
            private ITextSnapshot viewTextSnapshot;
            private ITextSnapshot viewVisualSnapshot;

            public event EventHandler<TaggerEventArgs> Changed;
            public event EventHandler UIUpdatesPaused { add { } remove { } }
            public event EventHandler UIUpdatesResumed { add { } remove { } }

            public ViewSpanChangedEventSource(ITextView textView, TaggerDelay textChangeDelay, TaggerDelay scrollChangeDelay)
            {
                Debug.Assert(textView != null);
                this.textView = textView;
                this.textChangeDelay = textChangeDelay;
                this.scrollChangeDelay = scrollChangeDelay;
            }

            public void Connect()
            {
                foregroundObject.AssertIsForeground();
                textView.LayoutChanged += OnLayoutChanged;
            }

            public void Disconnect()
            {
                foregroundObject.AssertIsForeground();
                textView.LayoutChanged -= OnLayoutChanged;
            }

            private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
            {
                foregroundObject.AssertIsForeground();
                // The formatted span refers to the span of the textview's buffer that is visible.
                // If it changes, then we want to reclassify.  Note: the span might not change if
                // text were overwritten.  However, in the case of text-edits, we'll hear about 
                // through other means as we have an EventSource for that purpose.  This event
                // source is for knowing if the user moves the view around.  This handles direct
                // moves using the caret/scrollbar, as well as moves that happen because someone
                // jumped directly to a location using goto-def.  It also handles view changes
                // caused by the user collapsing an outlining region.

                var lastSpan = this.span;
                var lastViewTextSnapshot = this.viewTextSnapshot;
                var lastViewVisualSnapshot = this.viewVisualSnapshot;

                this.span = textView.TextViewLines.FormattedSpan.Span;
                this.viewTextSnapshot = textView.TextSnapshot;
                this.viewVisualSnapshot = textView.VisualSnapshot;

                if (this.span != lastSpan)
                {
                    // The span changed.  This could have happened for a few different reasons.  
                    // If none of the view's text snapshots changed, then it was because of scrolling.

                    if (this.viewTextSnapshot == lastViewTextSnapshot &&
                        this.viewVisualSnapshot == lastViewVisualSnapshot)
                    {
                        // We scrolled.
                        RaiseChanged(scrollChangeDelay);
                    }
                    else
                    {
                        // text changed in some way.
                        RaiseChanged(textChangeDelay);
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