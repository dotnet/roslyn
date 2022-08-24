// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [Export(typeof(IPreviewFactoryService)), Shared]
    internal class PreviewFactoryService : AbstractPreviewFactoryService<IWpfDifferenceViewer>
    {
        private readonly IWpfDifferenceViewerFactoryService _differenceViewerService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreviewFactoryService(
            IThreadingContext threadingContext,
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextDifferencingSelectorService differenceSelectorService,
            IDifferenceBufferFactoryService differenceBufferService,
            IWpfDifferenceViewerFactoryService differenceViewerService,
            IGlobalOptionService globalOptions)
            : base(threadingContext,
                  textBufferFactoryService,
                  contentTypeRegistryService,
                  projectionBufferFactoryService,
                  editorOptionsFactoryService,
                  differenceSelectorService,
                  differenceBufferService,
                  textEditorFactoryService.CreateTextViewRoleSet(
                      TextViewRoles.PreviewRole, PredefinedTextViewRoles.Analyzable),
                  globalOptions)
        {
            _differenceViewerService = differenceViewerService;
        }

        protected override async Task<IWpfDifferenceViewer> CreateDifferenceViewAsync(IDifferenceBuffer diffBuffer, ITextViewRoleSet previewRoleSet, DifferenceViewMode mode, double zoomLevel, CancellationToken cancellationToken)
        {
            var diffViewer = _differenceViewerService.CreateDifferenceView(diffBuffer, previewRoleSet);

            const string DiffOverviewMarginName = "deltadifferenceViewerOverview";

            diffViewer.ViewMode = mode;

            if (mode == DifferenceViewMode.RightViewOnly)
            {
                diffViewer.RightView.ZoomLevel *= zoomLevel;
                diffViewer.RightHost.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Visibility = Visibility.Collapsed;
            }
            else if (mode == DifferenceViewMode.LeftViewOnly)
            {
                diffViewer.LeftView.ZoomLevel *= zoomLevel;
                diffViewer.LeftHost.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Visibility = Visibility.Collapsed;
            }
            else
            {
                Contract.ThrowIfFalse(mode == DifferenceViewMode.Inline);
                diffViewer.InlineView.ZoomLevel *= zoomLevel;
                diffViewer.InlineHost.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Visibility = Visibility.Collapsed;
            }

            // Disable focus / tab stop for the diff viewer.
            diffViewer.RightView.VisualElement.Focusable = false;
            diffViewer.LeftView.VisualElement.Focusable = false;
            diffViewer.InlineView.VisualElement.Focusable = false;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
            await diffViewer.SizeToFitAsync(ThreadingContext, cancellationToken: cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

            return diffViewer;
        }
    }
}
