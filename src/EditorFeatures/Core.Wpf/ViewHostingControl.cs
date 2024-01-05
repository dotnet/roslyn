// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal class ViewHostingControl : ContentControl
    {
        private readonly Func<ITextBuffer, IWpfTextView> _createView;
        private readonly Func<ITextBuffer> _createBuffer;

        private ITextBuffer _createdTextBuffer;
        private IWpfTextView _createdView;

        public ViewHostingControl(
            Func<ITextBuffer, IWpfTextView> createView,
            Func<ITextBuffer> createBuffer)
        {
            _createView = createView;
            _createBuffer = createBuffer;

            Background = Brushes.Transparent;
            this.IsVisibleChanged += OnIsVisibleChanged;
        }

        private void EnsureBufferCreated()
        {
            _createdTextBuffer ??= _createBuffer();
        }

        private void EnsureContentCreated()
        {
            if (this.Content == null)
            {
                EnsureBufferCreated();
                _createdView = _createView(_createdTextBuffer);
                this.Content = _createdView.VisualElement;
            }
        }

        public ITextView TextView_TestOnly
        {
            get
            {
                EnsureContentCreated();
                return (ITextView)this.Content;
            }
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var nowVisible = (bool)e.NewValue;
            if (nowVisible)
            {
                EnsureContentCreated();
            }
            else
            {
                this.Content = null;

                _createdView.Close();
                _createdView = null;

                // If a projection buffer has a source span from another buffer, the projection buffer is held alive by the other buffer too.
                // This means that a one-off projection buffer created for a tooltip would be kept alive as long as the underlying file
                // is still open. Removing the source spans from the projection buffer ensures the projection buffer can be GC'ed.
                if (_createdTextBuffer is IProjectionBuffer projectionBuffer)
                {
                    projectionBuffer.DeleteSpans(0, projectionBuffer.CurrentSnapshot.SpanCount);
                }

                _createdTextBuffer = null;
            }
        }

        public string GetText_TestOnly()
        {
            EnsureBufferCreated();
            return _createdTextBuffer.CurrentSnapshot.GetText();
        }
    }
}
