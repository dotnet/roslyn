// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract class AbstractRenameTagger<T> : ITagger<T>, IDisposable where T : ITag
{
    private readonly ITextBuffer _buffer;
    private readonly InlineRenameService _renameService;

    private InlineRenameSession.OpenTextBufferManager _bufferManager;
    private IEnumerable<RenameTrackingSpan> _currentSpans;

    protected AbstractRenameTagger(ITextBuffer buffer, InlineRenameService renameService)
    {
        _buffer = buffer;
        _renameService = renameService;

        _renameService.ActiveSessionChanged += OnActiveSessionChanged;

        if (_renameService.ActiveSession != null)
        {
            AttachToSession(_renameService.ActiveSession);
        }
    }

    private void OnActiveSessionChanged(object sender, InlineRenameService.ActiveSessionChangedEventArgs e)
    {
        if (e.PreviousSession != null)
        {
            DetachFromSession();
        }

        if (_renameService.ActiveSession != null)
        {
            AttachToSession(_renameService.ActiveSession);
        }
    }

    private void AttachToSession(InlineRenameSession session)
    {
        if (session.TryGetBufferManager(_buffer, out _bufferManager))
        {
            _bufferManager.SpansChanged += OnSpansChanged;
            OnSpansChanged();
        }
    }

    private void DetachFromSession()
    {
        if (_bufferManager != null)
        {
            RaiseTagsChangedForEntireBuffer();

            _bufferManager.SpansChanged -= OnSpansChanged;
            _bufferManager = null;
            _currentSpans = null;
        }
    }

    private void OnSpansChanged()
    {
        _currentSpans = _bufferManager.GetRenameTrackingSpans();
        RaiseTagsChangedForEntireBuffer();
    }

    private void RaiseTagsChangedForEntireBuffer()
        => TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(_buffer.CurrentSnapshot.GetFullSpan()));

    public void Dispose()
    {
        _renameService.ActiveSessionChanged -= OnActiveSessionChanged;

        if (_renameService.ActiveSession != null)
        {
            DetachFromSession();
        }
    }

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    IEnumerable<ITagSpan<T>> ITagger<T>.GetTags(NormalizedSnapshotSpanCollection spans)
        => GetTags(spans);

    public IEnumerable<TagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans)
    {
        if (_renameService.ActiveSession == null)
        {
            yield break;
        }

        var renameSpans = _currentSpans;
        if (renameSpans != null)
        {
            var snapshot = spans.First().Snapshot;
            foreach (var renameSpan in renameSpans)
            {
                var span = renameSpan.TrackingSpan.GetSpan(snapshot);
                if (spans.OverlapsWith(span))
                {
                    if (TryCreateTagSpan(span, renameSpan.Type, out var tagSpan))
                    {
                        yield return tagSpan;
                    }
                }
            }
        }
    }

    protected abstract bool TryCreateTagSpan(SnapshotSpan span, RenameSpanKind type, out TagSpan<T> tagSpan);
}
