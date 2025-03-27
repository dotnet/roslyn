// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor;

[Export(typeof(ITextViewConnectionListener))]
[ContentType(ContentTypeNames.RoslynContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
[Export(typeof(ITextBufferAssociatedViewService))]
internal sealed class TextBufferAssociatedViewService : ITextViewConnectionListener, ITextBufferAssociatedViewService
{
#if DEBUG
    private static readonly HashSet<ITextView> s_registeredViews = [];
#endif

    private static readonly object s_gate = new();
    private static readonly ConditionalWeakTable<ITextBuffer, HashSet<ITextView>> s_map = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TextBufferAssociatedViewService()
    {
    }

    public event EventHandler<SubjectBuffersConnectedEventArgs> SubjectBuffersConnected;
    public event EventHandler<SubjectBuffersConnectedEventArgs> SubjectBuffersDisconnected;

    void ITextViewConnectionListener.SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
    {
        lock (s_gate)
        {
            // only add roslyn type to tracking map
            foreach (var buffer in subjectBuffers.Where(b => IsSupportedContentType(b.ContentType)))
            {
                if (!s_map.TryGetValue(buffer, out var set))
                {
                    set = [];
                    s_map.Add(buffer, set);
                }

                set.Add(textView);
                DebugRegisterView_NoLock(textView);
            }
        }

        this.SubjectBuffersConnected?.Invoke(this, new SubjectBuffersConnectedEventArgs(textView, subjectBuffers.ToReadOnlyCollection()));
    }

    void ITextViewConnectionListener.SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
    {
        lock (s_gate)
        {
            // we need to check all buffers reported since we will be called after actual changes have happened. 
            // for example, if content type of a buffer changed, we will be called after it is changed, rather than before it.
            foreach (var buffer in subjectBuffers)
            {
                if (s_map.TryGetValue(buffer, out var set))
                {
                    set.Remove(textView);
                    if (set.Count == 0)
                    {
                        s_map.Remove(buffer);
                    }
                }
            }
        }

        this.SubjectBuffersDisconnected?.Invoke(this, new SubjectBuffersConnectedEventArgs(textView, subjectBuffers.ToReadOnlyCollection()));
    }

    private static bool IsSupportedContentType(IContentType contentType)
    {
        // This list should match the list of exported content types above
        return contentType.IsOfType(ContentTypeNames.RoslynContentType) ||
               contentType.IsOfType(ContentTypeNames.XamlContentType);
    }

    private static IList<ITextView> GetTextViews(ITextBuffer textBuffer)
    {
        lock (s_gate)
        {
            if (!s_map.TryGetValue(textBuffer, out var set))
            {
                return [];
            }

            return [.. set];
        }
    }

    public IEnumerable<ITextView> GetAssociatedTextViews(ITextBuffer textBuffer)
        => GetTextViews(textBuffer);

    private static bool HasFocus(ITextView textView)
        => textView.HasAggregateFocus;

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
    private static void DebugRegisterView_NoLock(ITextView textView)
    {
#if DEBUG
        if (s_registeredViews.Add(textView))
        {
            textView.Closed += OnTextViewClose;
        }
#endif
    }

#if DEBUG
    private static void OnTextViewClose(object sender, EventArgs e)
    {
        var view = sender as ITextView;

        lock (s_gate)
        {
            foreach (var buffer in view.BufferGraph.GetTextBuffers(b => IsSupportedContentType(b.ContentType)))
            {
                if (s_map.TryGetValue(buffer, out var set))
                {
                    Contract.ThrowIfTrue(set.Contains(view));
                }
            }

            s_registeredViews.Remove(view);
        }
    }
#endif
}
