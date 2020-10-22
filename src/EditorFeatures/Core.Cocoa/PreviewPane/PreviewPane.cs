using System;
using System.Collections.Generic;
using AppKit;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane
{
    class PreviewPane : NSView
    {
        private NSImage severityIcon;
        private string id;
        private string title;
        private string description;
        private Uri helpLink;
        private string helpLinkToolTipText;
        private IReadOnlyList<object> previewContent;
        private bool logIdVerbatimInTelemetry;
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
            _differenceViewerPreview = (DifferenceViewerPreview) previewContent[0];
            var view = _differenceViewerPreview.Viewer.VisualElement;
            SetFrameSize(view.Frame.Size);
            AddSubview(view);

            // HACK: This is here for a11y compliance and should be removed as
            // we find a better alternative
            this.AccessibilityHelp = _differenceViewerPreview?.Viewer?.DifferenceBuffer?.InlineBuffer?.CurrentSnapshot.GetText();
        }

        PreviewPane(IntPtr ptr)
            : base(ptr)
        {
        }

        protected override void Dispose(bool disposing)
        {
            _differenceViewerPreview?.Dispose();
            _differenceViewerPreview = null;
        }
    }
}
