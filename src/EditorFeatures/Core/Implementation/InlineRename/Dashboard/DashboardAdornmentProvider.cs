// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class DashboardAdornmentProvider : IWpfTextViewConnectionListener
    {
        private readonly InlineRenameService _renameService;

        public const string AdornmentLayerName = "RoslynRenameDashboard";

        [Export]
        [Name(AdornmentLayerName)]
        [Order(After = PredefinedAdornmentLayers.Outlining)]
        [Order(After = PredefinedAdornmentLayers.Text)]
        [Order(After = PredefinedAdornmentLayers.Selection)]
        [Order(After = PredefinedAdornmentLayers.Caret)]
        [Order(After = PredefinedAdornmentLayers.TextMarker)]
        [Order(After = PredefinedAdornmentLayers.CurrentLineHighlighter)]
        [Order(After = PredefinedAdornmentLayers.Squiggle)]
        internal readonly AdornmentLayerDefinition AdornmentLayer;

        [ImportingConstructor]
        public DashboardAdornmentProvider(
            InlineRenameService renameService)
        {
            _renameService = renameService;
        }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            // Create it for the view if we don't already have one
            textView.GetOrCreateAutoClosingProperty(v => new DashboardAdornmentManager(_renameService, v));
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            // Do we still have any buffers alive?
            if (textView.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType)).Any())
            {
                // Yep, some are still attached
                return;
            }

            DashboardAdornmentManager manager;
            if (textView.Properties.TryGetProperty(typeof(DashboardAdornmentManager), out manager))
            {
                manager.Dispose();
                textView.Properties.RemoveProperty(typeof(DashboardAdornmentManager));
            }
        }
    }
}
