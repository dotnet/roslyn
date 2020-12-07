// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using AppKit;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal class ViewHostingControl : NSView
    {
        private readonly Func<ITextBuffer, ICocoaTextView> _createView;
        private readonly Func<ITextBuffer> _createBuffer;

        private ITextBuffer _createdTextBuffer;
        private ICocoaTextView _createdView;

        public ViewHostingControl(
            Func<ITextBuffer, ICocoaTextView> createView,
            Func<ITextBuffer> createBuffer)
        {
            _createView = createView;
            _createBuffer = createBuffer;

            //= Brushes.Transparent;
        }

        private void EnsureBufferCreated()
        {
            if (_createdTextBuffer == null)
            {
                _createdTextBuffer = _createBuffer();
            }
        }

        private void EnsureContentCreated()
        {
            if (this.Subviews.Length == 0)
            {
                EnsureBufferCreated();
                _createdView = _createView(_createdTextBuffer);
                this.AddSubview(_createdView.VisualElement);
            }
        }

#pragma warning disable IDE0051 // Remove unused private members
        private void OnIsVisibleChanged(bool nowVisible)
#pragma warning restore IDE0051 // Remove unused private members
        {
            if (nowVisible)
            {
                EnsureContentCreated();
            }
            else
            {
                if (this.Subviews.Length > 0)
                    this.Subviews[0].RemoveFromSuperview();

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
    }
}
