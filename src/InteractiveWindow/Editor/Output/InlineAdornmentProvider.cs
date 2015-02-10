using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(IntraTextAdornmentTag))]
    [ContentType(PredefinedInteractiveContentTypes.InteractiveContentTypeName)]
    internal sealed class InlineAdornmentProvider : IViewTaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer == null || textView == null || typeof(T) != typeof(IntraTextAdornmentTag))
            {
                return null;
            }

            return (ITagger<T>)textView.Properties.GetOrCreateSingletonProperty<InlineReplAdornmentManager>(
                typeof(InlineReplAdornmentManager),
                () => new InlineReplAdornmentManager(textView));
        }

        private class InlineReplAdornmentManager : ITagger<IntraTextAdornmentTag>
        {
            private readonly ITextView textView;
            private readonly List<Tuple<int, ZoomableInlineAdornment>> tags;
            private readonly Dispatcher dispatcher;

            internal InlineReplAdornmentManager(ITextView textView)
            {
                this.textView = textView;
                this.tags = new List<Tuple<int, ZoomableInlineAdornment>>();
                this.dispatcher = Dispatcher.CurrentDispatcher;
            }

            public IEnumerable<ITagSpan<IntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
            {
                var result = new List<TagSpan<IntraTextAdornmentTag>>();
                foreach (var t in tags)
                {
                    var span = new SnapshotSpan(textView.TextSnapshot, t.Item1, 0);
                    t.Item2.UpdateSize();
                    var tag = new IntraTextAdornmentTag(t.Item2, null);
                    result.Add(new TagSpan<IntraTextAdornmentTag>(span, tag));
                }

                return result;
            }

            public void AddAdornment(ZoomableInlineAdornment uiElement)
            {
                if (Dispatcher.CurrentDispatcher != dispatcher)
                {
                    dispatcher.BeginInvoke(new Action(() => AddAdornment(uiElement)));
                    return;
                }

                var caretPos = textView.Caret.Position.BufferPosition;
                var caretLine = caretPos.GetContainingLine();
                tags.Add(new Tuple<int, ZoomableInlineAdornment>(caretPos.Position, uiElement));

                var handler = TagsChanged;
                if (handler != null)
                {
                    var span = new SnapshotSpan(textView.TextSnapshot, caretLine.Start, caretLine.Length);
                    var args = new SnapshotSpanEventArgs(span);
                    handler(this, args);
                }
            }

            public IList<Tuple<int, ZoomableInlineAdornment>> Adornments
            {
                get { return tags; }
            }

            public void RemoveAll()
            {
                tags.Clear();
            }

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        }

        private static InlineReplAdornmentManager GetManager(ITextView view)
        {
            InlineReplAdornmentManager result;
            if (!view.Properties.TryGetProperty<InlineReplAdornmentManager>(typeof(InlineReplAdornmentManager), out result))
            {
                return null;
            }

            return result;
        }

        public static void AddInlineAdornment(ITextView view, UIElement uiElement, RoutedEventHandler onLoaded)
        {
            var manager = GetManager(view);
            if (manager != null)
            {
                var adornment = new ZoomableInlineAdornment(uiElement, view);

                // Original Python code unhooked this event after load was complete
                // I don't think this should be needed... we'll see.
                adornment.Loaded += onLoaded;
                manager.AddAdornment(adornment);
            }
        }

        public static void ZoomInlineAdornments(ITextView view, double zoomFactor)
        {
            var manager = GetManager(view);
            if (manager != null)
            {
                foreach (var t in manager.Adornments)
                {
                    t.Item2.Zoom(zoomFactor);
                }
            }
        }

        public static void MinimizeLastInlineAdornment(ITextView view)
        {
            var manager = GetManager(view);
            if (manager != null && manager.Adornments.Count > 0)
            {
                var adornment = manager.Adornments[manager.Adornments.Count - 1].Item2;
                adornment.Zoom(adornment.MinimizedZoom);
            }
        }

        public static void RemoveAllAdornments(ITextView view)
        {
            var manager = GetManager(view);
            if (manager != null)
            {
                manager.RemoveAll();
            }
        }
    }
}
