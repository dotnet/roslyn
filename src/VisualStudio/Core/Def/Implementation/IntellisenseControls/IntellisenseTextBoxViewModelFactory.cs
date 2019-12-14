// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using ImportingConstructorAttribute = System.Composition.ImportingConstructorAttribute;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls
{
    [Export(typeof(IntellisenseTextBoxViewModelFactory)), Shared]
    internal class IntellisenseTextBoxViewModelFactory
    {
        private static readonly INITVIEW[] s_InitViews = new[] {
            new INITVIEW()
            {
                fSelectionMargin = 0,
                fWidgetMargin = 0,
                fDragDropMove = 0,
                IndentStyle = vsIndentStyle.vsIndentStyleNone
            }
        };

        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly OLE.Interop.IServiceProvider _serviceProvider;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IntellisenseTextBoxViewModelFactory(
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            SVsServiceProvider services)
        {
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _serviceProvider = (OLE.Interop.IServiceProvider)services.GetService(typeof(OLE.Interop.IServiceProvider));
            _projectionBufferFactoryService = projectionBufferFactoryService;
        }

        public async Task<IntellisenseTextBoxViewModel[]> CreateIntellisenseTextBoxViewModelsAsync(
          Document document,
          IContentType contentType,
          string contentString,
          Func<IProjectionSnapshot, ITrackingSpan[]> createSpansMethod,
          string[][] rolesCollections, CancellationToken cancellationToken)
        {
            IVsTextLines vsTextLines = _editorAdaptersFactoryService.CreateVsTextBufferAdapter(_serviceProvider, contentType) as IVsTextLines;
            vsTextLines.InitializeContent(contentString, contentString.Length);

            var originalContextBuffer = _editorAdaptersFactoryService.GetDataBuffer(vsTextLines);
            // Get the workspace, and from there, the solution and document containing this buffer.
            // If there's an ExternalSource, we won't get a document. Give up in that case.
            var solution = document.Project.Solution;

            // Wrap the original ContextBuffer in a projection buffer that we can make read-only
            var contextBuffer = _projectionBufferFactoryService.CreateProjectionBuffer(null,
                new object[] { originalContextBuffer.CurrentSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive) }, ProjectionBufferOptions.None, contentType);

            // Make projection readonly so we can't edit it by mistake.
            using (var regionEdit = contextBuffer.CreateReadOnlyRegionEdit())
            {
                regionEdit.CreateReadOnlyRegion(new Span(0, contextBuffer.CurrentSnapshot.Length), SpanTrackingMode.EdgeInclusive, EdgeInsertionMode.Deny);
                regionEdit.Apply();
            }

            // Put it all into a projection buffer
            var projectionBuffer = _projectionBufferFactoryService.CreateProjectionBuffer(null,
                createSpansMethod(contextBuffer.CurrentSnapshot),
                ProjectionBufferOptions.None, contentType);

            // Fork the solution using this new primary buffer for the document and all of its linked documents.
            var forkedSolution = solution.WithDocumentText(document.Id, projectionBuffer.CurrentSnapshot.AsText(), PreservationMode.PreserveIdentity);
            foreach (var link in document.GetLinkedDocumentIds())
            {
                forkedSolution = forkedSolution.WithDocumentText(link, projectionBuffer.CurrentSnapshot.AsText(), PreservationMode.PreserveIdentity);
            }

            // Put it into a new workspace, and open it and its related documents
            // with the projection buffer as the text.
            var workspace = new IntellisenseTextBoxWorkspace(forkedSolution, document.Project);
            workspace.OpenDocument(workspace.ChangeSignatureDocumentId, originalContextBuffer.AsTextContainer());
            foreach (var link in document.GetLinkedDocumentIds())
            {
                workspace.OpenDocument(link, originalContextBuffer.AsTextContainer());
            }

            // Start getting the compilation so the PartialSolution will be ready when the user starts typing in the window
            await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            _editorAdaptersFactoryService.SetDataBuffer(vsTextLines, projectionBuffer);

            IntellisenseTextBoxViewModel[] result = new IntellisenseTextBoxViewModel[rolesCollections.Length];
            for (int i = 0; i < rolesCollections.Length; i++)
            {
                ITextViewRoleSet roleSet = _textEditorFactoryService.CreateTextViewRoleSet(rolesCollections[i]);

                IVsTextView vsTextView = _editorAdaptersFactoryService.CreateVsTextViewAdapter(_serviceProvider, roleSet);

                vsTextView.Initialize(
                    vsTextLines,
                    IntPtr.Zero,
                    (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT,
                    s_InitViews);

                // Here we need ITextViewModelProvider handling the corresponding role.
                IWpfTextView wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(vsTextView);
                wpfTextView.TextBuffer.ChangeContentType(contentType, null);

                result[i] = new IntellisenseTextBoxViewModel(vsTextView, wpfTextView);
            }

            return result;
        }
    }
}
