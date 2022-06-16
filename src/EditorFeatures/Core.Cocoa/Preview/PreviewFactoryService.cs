// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
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
    internal class PreviewFactoryService : AbstractPreviewFactoryService<ICocoaDifferenceViewer>
    {
        private readonly ICocoaDifferenceViewerFactoryService _differenceViewerService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreviewFactoryService(
            IThreadingContext threadingContext,
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            ICocoaTextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextDifferencingSelectorService differenceSelectorService,
            IDifferenceBufferFactoryService differenceBufferService,
            ICocoaDifferenceViewerFactoryService differenceViewerService,
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

        protected override async Task<ICocoaDifferenceViewer> CreateDifferenceViewAsync(IDifferenceBuffer diffBuffer, ITextViewRoleSet previewRoleSet, DifferenceViewMode mode, double zoomLevel, CancellationToken cancellationToken)
        {
            var diffViewer = _differenceViewerService.CreateDifferenceView(diffBuffer, previewRoleSet);
            diffViewer.ViewMode = mode;
            const string DiffOverviewMarginName = "deltadifferenceViewerOverview";
            if (mode == DifferenceViewMode.RightViewOnly)
            {
                diffViewer.RightHost.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Hidden = true;
            }
            else if (mode == DifferenceViewMode.LeftViewOnly)
            {
                diffViewer.LeftHost.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Hidden = true;
            }
            else
            {
                Contract.ThrowIfFalse(mode == DifferenceViewMode.Inline);
                diffViewer.InlineHost.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Hidden = true;
            }

            // We use ConfigureAwait(true) to stay on the UI thread.
            await diffViewer.SizeToFitAsync(ThreadingContext, cancellationToken: cancellationToken).ConfigureAwait(true);

            return diffViewer;
        }
    }
}
