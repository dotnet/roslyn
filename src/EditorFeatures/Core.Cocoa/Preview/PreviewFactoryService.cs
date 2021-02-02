// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [Export(typeof(IPreviewFactoryService)), Shared]
    internal class PreviewFactoryService : AbstractPreviewFactoryService<ICocoaDifferenceViewer>, IPreviewFactoryService
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
            ICocoaDifferenceViewerFactoryService differenceViewerService)
            : base(threadingContext,
                  textBufferFactoryService,
                  contentTypeRegistryService,
                  projectionBufferFactoryService,
                  editorOptionsFactoryService,
                  differenceSelectorService,
                  differenceBufferService,
                  textEditorFactoryService.CreateTextViewRoleSet(
                      TextViewRoles.PreviewRole, PredefinedTextViewRoles.Analyzable))
        {
            _differenceViewerService = differenceViewerService;
        }

        protected override async Task<ICocoaDifferenceViewer> CreateDifferenceViewAsync(IDifferenceBuffer diffBuffer, ITextViewRoleSet previewRoleSet, DifferenceViewMode mode, double zoomLevel, CancellationToken cancellationToken)
        {
            var diffViewer = _differenceViewerService.CreateDifferenceView(diffBuffer, previewRoleSet);
            diffViewer.ViewMode = mode;

            // This code path must be invoked on UI thread.
            AssertIsForeground();

            // We use ConfigureAwait(true) to stay on the UI thread.
            await diffViewer.SizeToFitAsync(ThreadingContext).ConfigureAwait(true);

            return diffViewer;
        }
    }
}
