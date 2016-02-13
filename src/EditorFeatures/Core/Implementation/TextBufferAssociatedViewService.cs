// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    [Export(typeof(ITextBufferAssociatedViewService))]
    internal class TextBufferAssociatedViewService : IWpfTextViewConnectionListener, ITextBufferAssociatedViewService
    {
#if DEBUG
        private static readonly HashSet<IWpfTextView> s_registeredViews = new HashSet<IWpfTextView>();
#endif

        private static readonly object s_gate = new object();
        private static readonly ConditionalWeakTable<ITextBuffer, HashSet<IWpfTextView>> s_map =
            new ConditionalWeakTable<ITextBuffer, HashSet<IWpfTextView>>();

        public event EventHandler<SubjectBuffersConnectedEventArgs> SubjectBuffersConnected;

        void IWpfTextViewConnectionListener.SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            lock (s_gate)
            {
                // only add roslyn type to tracking map
                foreach (var buffer in subjectBuffers.Where(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType)))
                {
                    HashSet<IWpfTextView> set;
                    if (!s_map.TryGetValue(buffer, out set))
                    {
                        set = new HashSet<IWpfTextView>();
                        s_map.Add(buffer, set);
                    }

                    set.Add(textView);
                    DebugRegisterView_NoLock(textView);
                }
            }
            this.SubjectBuffersConnected?.Invoke(this, new SubjectBuffersConnectedEventArgs(textView, subjectBuffers.ToReadOnlyCollection()));
        }

        void IWpfTextViewConnectionListener.SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            lock (s_gate)
            {
                // we need to check all buffers reported since we will be called after actual changes have happened. 
                // for example, if content type of a buffer changed, we will be called after it is changed, rather than before it.
                foreach (var buffer in subjectBuffers)
                {
                    HashSet<IWpfTextView> set;
                    if (s_map.TryGetValue(buffer, out set))
                    {
                        set.Remove(textView);
                        if (set.Count == 0)
                        {
                            s_map.Remove(buffer);
                        }
                    }
                }
            }
        }

        private static IList<IWpfTextView> GetTextViews(ITextBuffer textBuffer)
        {
            lock (s_gate)
            {
                HashSet<IWpfTextView> set;
                if (!s_map.TryGetValue(textBuffer, out set))
                {
                    return SpecializedCollections.EmptyList<IWpfTextView>();
                }

                return set.ToList();
            }
        }

        public IEnumerable<IWpfTextView> GetAssociatedTextViews(ITextBuffer textBuffer)
        {
            return GetTextViews(textBuffer);
        }

        private static bool HasFocus(IWpfTextView textView)
        {
            return textView.HasAggregateFocus;
        }

        public static bool AnyAssociatedViewHasFocus(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                return false;
            }

            var views = GetTextViews(textBuffer);
            if (views.Count == 0)
            {
                // We haven't seen the view yet.  Assume it is visible.
                return true;
            }

            return views.Any(HasFocus);
        }

        [Conditional("DEBUG")]
        private void DebugRegisterView_NoLock(IWpfTextView textView)
        {
#if DEBUG
            if (s_registeredViews.Add(textView))
            {
                textView.Closed += OnTextViewClose;
            }
#endif
        }

#if DEBUG
        private void OnTextViewClose(object sender, EventArgs e)
        {
            var view = sender as IWpfTextView;

            lock (s_gate)
            {
                foreach (var buffer in view.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType)))
                {
                    HashSet<IWpfTextView> set;
                    if (s_map.TryGetValue(buffer, out set))
                    {
                        Contract.ThrowIfTrue(set.Contains(view));
                    }
                }

                s_registeredViews.Remove(view);
            }
        }
#endif
    }
}
