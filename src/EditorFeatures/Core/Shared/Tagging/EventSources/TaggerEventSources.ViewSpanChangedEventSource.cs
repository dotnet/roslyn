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
        private class ViewSpanChangedEventSource : ForegroundThreadAffinitizedObject, ITaggerEventSource
        {
            private readonly ITextView textView;
            private readonly TaggerDelay delay;

            private Span? lastSpan;

            public ViewSpanChangedEventSource(ITextView textView, TaggerDelay delay)
            {
                this.AssertIsForeground();
                Debug.Assert(textView != null);
                this.textView = textView;
                this.delay = delay;
            }

            public event EventHandler<TaggerEventArgs> Changed;

#pragma warning disable CS0067
            public event EventHandler UIUpdatesPaused;
            public event EventHandler UIUpdatesResumed;
#pragma warning restore

            public void Connect()
            {
                this.AssertIsForeground();
                textView.LayoutChanged += OnLayoutChanged;
            }

            public void Disconnect()
            {
                this.AssertIsForeground();
                textView.LayoutChanged += OnLayoutChanged;
            }

            private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
            {
                this.AssertIsForeground();
                var changed = this.Changed;
                if (changed == null)
                {
                    return;
                }

                // The formatted span refers to the span of the textview's buffer that is visible.
                // If it changes, then we want to reclassify.  Note: the span might not change if
                // text were overwritten.  However, in the case of text-edits, we'll hear about 
                // through other means as we have an EventSource for that purpose.  This event
                // source is for knowing if the user moves the view around.  This handles direct
                // moves using the caret/scrollbar, as well as moves that happen because someone
                // jumped directly to a location using goto-def.  It also handles view changes
                // caused by the user collapsing an outlining region.
                var span = textView.TextViewLines.FormattedSpan.Span;

                if (span != lastSpan)
                {
                    this.lastSpan = span;
                    changed(this, new TaggerEventArgs(delay));
                }
            }
        }
    }
}