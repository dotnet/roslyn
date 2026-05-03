// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Roslyn.Hosting.Diagnostics.VenusMargin;

internal sealed class VenusMargin : IWpfTextViewMargin
{
    public const string MarginName = "VenusMargin";

    private readonly IProjectionBuffer _projectionBuffer;
    private readonly ProjectionBufferViewModel _viewModel = new();
    private readonly ProjectionBufferMargin _control;

    private bool _isDisposed = false;

    public VenusMargin(IWpfTextView textView, ITextEditorFactoryService textEditorFactory)
    {
        _projectionBuffer = (IProjectionBuffer)textView.TextBuffer;

        _control = new ProjectionBufferMargin
        {
            DataContext = _viewModel,
            TextEditorFactory = textEditorFactory,
            TextView = textView,
        };

        _projectionBuffer.Changed += OnProjectionBufferChanged;
        _projectionBuffer.SourceSpansChanged += this.OnProjectionBufferSourceSpansChanged;
        _projectionBuffer.SourceBuffersChanged += OnProjectionBufferSourceBuffersChanged;

        foreach (var b in _projectionBuffer.SourceBuffers)
        {
            _viewModel.SourceBuffers.Add(b);
        }
    }

    private void OnProjectionBufferSourceBuffersChanged(object sender, ProjectionSourceBuffersChangedEventArgs e)
    {
        foreach (var b in e.RemovedBuffers)
        {
            _viewModel.SourceBuffers.Remove(b);
        }

        foreach (var b in e.AddedBuffers)
        {
            _viewModel.SourceBuffers.Add(b);
        }

        UpdateSourceSpans();
    }

    private void OnProjectionBufferSourceSpansChanged(object sender, ProjectionSourceSpansChangedEventArgs e)
    {
        UpdateSourceSpans();
    }

    private void OnProjectionBufferChanged(object sender, TextContentChangedEventArgs e)
    {
        UpdateSourceSpans();
    }

    private void UpdateSourceSpans()
    {
        _viewModel.SourceSpans.Clear();
        foreach (var ss in _projectionBuffer.CurrentSnapshot.GetSourceSpans())
        {
            _viewModel.SourceSpans.Add(ss);
        }

        _viewModel.SourceSpans.Add(new SnapshotSpan(_projectionBuffer.CurrentSnapshot, 0, 0));
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(MarginName);
        }
    }

    public System.Windows.FrameworkElement VisualElement
    {
        get
        {
            ThrowIfDisposed();
            return _control;
        }
    }

    public double MarginSize
    {
        get
        {
            ThrowIfDisposed();
            return _control.ActualHeight;
        }
    }

    public bool Enabled
    {
        get
        {
            ThrowIfDisposed();
            return true;
        }
    }

    public ITextViewMargin GetTextViewMargin(string marginName)
    {
        return marginName == MarginName ? this : null;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
        }
    }
}
