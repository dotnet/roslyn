// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class InlineRenameAdornmentProvider : IWpfTextViewConnectionListener
    {
        private readonly InlineRenameService _renameService;
        private readonly IEditorFormatMapService _editorFormatMapService;
        private readonly IInlineRenameColorUpdater? _dashboardColorUpdater;
        private readonly IGlobalOptionService _globalOptionService;
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
        internal readonly AdornmentLayerDefinition? AdornmentLayer;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineRenameAdornmentProvider(
            InlineRenameService renameService,
            IEditorFormatMapService editorFormatMapService,
            [Import(AllowDefault = true)] IInlineRenameColorUpdater? dashboardColorUpdater,
            IGlobalOptionService globalOptionService)
        {
            _renameService = renameService;
            _editorFormatMapService = editorFormatMapService;
            _dashboardColorUpdater = dashboardColorUpdater;
            _globalOptionService = globalOptionService;
        }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            // Create it for the view if we don't already have one
            textView.GetOrCreateAutoClosingProperty(v => new InlineRenameAdornmentManager(_renameService, _editorFormatMapService, _dashboardColorUpdater, v, _globalOptionService));
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            // Do we still have any buffers alive?
            if (textView.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType)).Any())
            {
                // Yep, some are still attached
                return;
            }

            if (textView.Properties.TryGetProperty(typeof(InlineRenameAdornmentManager), out InlineRenameAdornmentManager manager))
            {
                manager.Dispose();
                textView.Properties.RemoveProperty(typeof(InlineRenameAdornmentManager));
            }
        }
    }
}
