// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using AppKit;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;

#pragma warning disable IDE0052 // Remove unread private members

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane
{
    internal class PreviewPane : NSView
    {
        private readonly NSImage severityIcon;
        private readonly string id;
        private readonly string title;
        private readonly string description;
        private readonly Uri helpLink;
        private readonly string helpLinkToolTipText;
        private readonly IReadOnlyList<object> previewContent;
        private readonly bool logIdVerbatimInTelemetry;
        private Guid? optionPageGuid;
        private DifferenceViewerPreview _differenceViewerPreview;

        public PreviewPane(NSImage severityIcon, string id, string title, string description, Uri helpLink, string helpLinkToolTipText, IReadOnlyList<object> previewContent, bool logIdVerbatimInTelemetry, Guid? optionPageGuid = null)
        {
            this.severityIcon = severityIcon;
            this.id = id;
            this.title = title;
            this.description = description;
            this.helpLink = helpLink;
            this.helpLinkToolTipText = helpLinkToolTipText;
            this.previewContent = previewContent;
            this.logIdVerbatimInTelemetry = logIdVerbatimInTelemetry;
            this.optionPageGuid = optionPageGuid;
            _differenceViewerPreview = (DifferenceViewerPreview)previewContent[0];
            var view = _differenceViewerPreview.Viewer.VisualElement;
            SetFrameSize(view.Frame.Size);
            AddSubview(view);

            // HACK: This is here for a11y compliance and should be removed as
            // we find a better alternative
            this.AccessibilityHelp = _differenceViewerPreview?.Viewer?.DifferenceBuffer?.InlineBuffer?.CurrentSnapshot.GetText();
        }

        protected override void Dispose(bool disposing)
        {
            _differenceViewerPreview?.Dispose();
            _differenceViewerPreview = null;
        }
    }
}
