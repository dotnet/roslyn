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
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Preview
{
    [Export(typeof(IPreviewFactoryService)), Shared]
    internal class PreviewFactoryService : AbstractPreviewFactoryService<IWpfDifferenceViewer>
    {
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly IWpfDifferenceViewerFactoryService _differenceViewerService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreviewFactoryService(
            IThreadingContext threadingContext,
            ITextBufferFactoryService textBufferFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            EditorOptionsService editorOptionsService,
            ITextDifferencingSelectorService differenceSelectorService,
            IDifferenceBufferFactoryService differenceBufferService,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            ITextDocumentFactoryService textDocumentFactoryService,
            IWpfDifferenceViewerFactoryService differenceViewerService)
            : base(threadingContext,
                  textBufferFactoryService,
                  contentTypeRegistryService,
                  projectionBufferFactoryService,
                  editorOptionsService,
                  differenceSelectorService,
                  differenceBufferService,
                  textDocumentFactoryService,
                  textEditorFactoryService.CreateTextViewRoleSet(
                      TextViewRoles.PreviewRole, PredefinedTextViewRoles.Analyzable, PredefinedTextViewRoles.Interactive, PredefinedTextViewRoles.ChangePreview))
        {
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _differenceViewerService = differenceViewerService;
        }

        protected override IDifferenceViewerPreview<IWpfDifferenceViewer> CreateDifferenceViewerPreview(IWpfDifferenceViewer viewer)
            => new DifferenceViewerPreview(viewer, _editorOperationsFactoryService);

        protected override async Task<IWpfDifferenceViewer> CreateDifferenceViewAsync(IDifferenceBuffer diffBuffer, ITextViewRoleSet previewRoleSet, DifferenceViewMode mode, double zoomLevel, CancellationToken cancellationToken)
        {
            var diffViewer = _differenceViewerService.CreateDifferenceView(diffBuffer, previewRoleSet);

            try
            {
                const string DiffOverviewMarginName = "deltadifferenceViewerOverview";

                diffViewer.ViewMode = mode;

                IWpfTextView view;
                IWpfTextViewHost host;
                if (mode == DifferenceViewMode.RightViewOnly)
                {
                    view = diffViewer.RightView;
                    host = diffViewer.RightHost;
                }
                else if (mode == DifferenceViewMode.LeftViewOnly)
                {
                    view = diffViewer.LeftView;
                    host = diffViewer.LeftHost;
                }
                else
                {
                    Contract.ThrowIfFalse(mode == DifferenceViewMode.Inline);
                    view = diffViewer.InlineView;
                    host = diffViewer.InlineHost;
                }

                view.ZoomLevel *= zoomLevel;
                view.Options.SetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarId, false);
                view.Options.SetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarId, false);
                view.Options.SetOptionValue(DefaultTextViewHostOptions.GlyphMarginId, false);
                view.Options.SetOptionValue(DefaultTextViewHostOptions.SelectionMarginId, false);
                view.Options.SetOptionValue(DefaultTextViewHostOptions.SuggestionMarginId, false);

                // Enable tab stop for the diff view host and collapse couple of unwanted margins.
                host.HostControl.IsTabStop = true;
                host.GetTextViewMargin(DiffOverviewMarginName).VisualElement.Visibility = Visibility.Collapsed;
                host.GetTextViewMargin(PredefinedMarginNames.Bottom).VisualElement.Visibility = Visibility.Collapsed;

                // Enable focus for the diff viewer.
                view.VisualElement.Focusable = true;

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task (containing method uses JTF)
                await diffViewer.SizeToFitAsync(ThreadingContext, cancellationToken: cancellationToken);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task

                return diffViewer;
            }
            catch
            {
                diffViewer.Close();
                throw;
            }
        }
    }
}
