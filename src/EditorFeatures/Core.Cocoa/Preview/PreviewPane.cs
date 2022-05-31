// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using AppKit;
using Foundation;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text.Differencing;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    internal class PreviewPane : NSView
    {
        private DifferenceViewerPreview? _differenceViewerPreview;
        private readonly NSTextField? titleField;

        public PreviewPane(string? id, string? title, Uri? helpLink, string? helpLinkToolTipText, IReadOnlyList<object> previewContent)
        {
            _differenceViewerPreview = (DifferenceViewerPreview)previewContent[0];
            var view = ((ICocoaDifferenceViewer)_differenceViewerPreview.Viewer).VisualElement;

            var originalSize = view.Frame.Size;

            this.TranslatesAutoresizingMaskIntoConstraints = false;

            // === Title ===
            // Title is in a stack view to help with padding
            var titlePlaceholder = new NSStackView()
            {
                Orientation = NSUserInterfaceLayoutOrientation.Vertical,
                EdgeInsets = new NSEdgeInsets(5, 0, 5, 0),
                Alignment = NSLayoutAttribute.Leading,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            // TODO: missing icon
            this.titleField = new NSTextField()
            {
                Editable = false,
                Bordered = false,
                BackgroundColor = NSColor.ControlBackground,
                DrawsBackground = false,
            };

            titlePlaceholder.AddArrangedSubview(titleField);
            AddSubview(titlePlaceholder);

            // === Preview View ===
            // This is the actual view, that shows the diff
            view.TranslatesAutoresizingMaskIntoConstraints = false;
            NSLayoutConstraint.Create(view, NSLayoutAttribute.Width, NSLayoutRelation.GreaterThanOrEqual, 1, originalSize.Width).Active = true;
            NSLayoutConstraint.Create(view, NSLayoutAttribute.Height, NSLayoutRelation.GreaterThanOrEqual, 1, originalSize.Height).Active = true;
            view.Subviews[0].TranslatesAutoresizingMaskIntoConstraints = false;
            view.WantsLayer = true;

            AddSubview(view);

            // === Constraints ===
            var constraints = new NSLayoutConstraint[]
            {
                // Title
                NSLayoutConstraint.Create(titlePlaceholder, NSLayoutAttribute.Top, NSLayoutRelation.Equal, this, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(titlePlaceholder, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, this, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(titlePlaceholder, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, this, NSLayoutAttribute.Trailing, 1, 0),

                // Preview View
                NSLayoutConstraint.Create(view, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, this, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(view, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, this, NSLayoutAttribute.Leading, 1, 0),
                NSLayoutConstraint.Create(view, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, this, NSLayoutAttribute.Trailing, 1, 0),

                // subviews
                NSLayoutConstraint.Create(view.Subviews[0], NSLayoutAttribute.Top, NSLayoutRelation.Equal, view, NSLayoutAttribute.Top, 1, 0),
                NSLayoutConstraint.Create(view.Subviews[0], NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, view, NSLayoutAttribute.Bottom, 1, 0),
                NSLayoutConstraint.Create(view.Subviews[0], NSLayoutAttribute.Left, NSLayoutRelation.Equal, view, NSLayoutAttribute.Left, 1, 0),
                NSLayoutConstraint.Create(view.Subviews[0], NSLayoutAttribute.Right, NSLayoutRelation.Equal, view, NSLayoutAttribute.Right, 1, 0),
            };

            if (GenerateAttributeString(id, title, helpLink, helpLinkToolTipText) is NSAttributedString attributedStringTitle)
            {
                this.titleField.AttributedStringValue = attributedStringTitle;
                // We do this separately, because the title sometimes isn't there (i.e. no diagnostics ID)
                // and we want the preview to stretch to the top
                NSLayoutConstraint.Create(view, NSLayoutAttribute.Top, NSLayoutRelation.Equal, titlePlaceholder, NSLayoutAttribute.Bottom, 1, 0).Active = true;
            }
            else
            {
                NSLayoutConstraint.Create(view, NSLayoutAttribute.Top, NSLayoutRelation.Equal, this, NSLayoutAttribute.Top, 1, 0).Active = true;
            }

            NSLayoutConstraint.ActivateConstraints(constraints);

            _differenceViewerPreview.Viewer.InlineView.TryMoveCaretToAndEnsureVisible(
                new Microsoft.VisualStudio.Text.SnapshotPoint(_differenceViewerPreview.Viewer.InlineView.TextSnapshot, 0));
        }

        public PreviewPane(IntPtr ptr)
            : base(ptr)
        {
        }

        private static NSAttributedString? GenerateAttributeString(string? id, string? title, Uri? link, string? linkTooltip)
        {
            if (string.IsNullOrEmpty(title))
                return null;

            var attributedBuffer = new NSMutableAttributedString();

            attributedBuffer.BeginEditing();

            var normalText = new NSStringAttributes
            {
                ForegroundColor = NSColor.ControlText
            };

            if (!string.IsNullOrEmpty(id))
            {
                var linkAttributes = new NSStringAttributes
                {
                    LinkUrl = link,
                    ToolTip = linkTooltip
                };

                attributedBuffer.Append(new NSAttributedString(id, linkAttributes));
                attributedBuffer.Append(new NSAttributedString(": ", normalText));
            }

            attributedBuffer.Append(new NSAttributedString(title, normalText));
            attributedBuffer.EndEditing();

            return attributedBuffer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _differenceViewerPreview?.Dispose();
                _differenceViewerPreview = null;
            }

            base.Dispose(disposing);
        }
    }
}
