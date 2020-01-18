// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly SVsServiceProvider _services;

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
            _services = services;
            _projectionBufferFactoryService = projectionBufferFactoryService;
        }

        public async Task<IntellisenseTextBoxViewModel[]?> CreateIntellisenseTextBoxViewModelsAsync(
          Document document,
          IContentType contentType,
          int insertPosition,
          string textToInsert,
          Func<ITextSnapshot, int, ITrackingSpan[]> createSpansMethod,
          string[][] rolesCollections)
        {
            var serviceProvider = (OLE.Interop.IServiceProvider)_services.GetService(typeof(OLE.Interop.IServiceProvider));

            var syntaxTree = await document.GetSyntaxTreeAsync(CancellationToken.None).ConfigureAwait(false);
            if (syntaxTree == null)
            {
                return null;
            }

            var sourceText = await syntaxTree.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
            var documentText = sourceText.ToString();

            var contentString = documentText.Insert(insertPosition, textToInsert);

            var vsTextLines = _editorAdaptersFactoryService.CreateVsTextBufferAdapter(serviceProvider, contentType) as IVsTextLines;
            if (vsTextLines == null)
            {
                return null;
            }

            vsTextLines.InitializeContent(contentString, contentString.Length);

            var originalContextBuffer = _editorAdaptersFactoryService.GetDataBuffer(vsTextLines);
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
                createSpansMethod(contextBuffer.CurrentSnapshot, insertPosition),
                ProjectionBufferOptions.None, contentType);

            // Fork the solution using this new primary buffer for the document and all of its linked documents.
            var forkedSolution = solution.WithDocumentText(document.Id, projectionBuffer.CurrentSnapshot.AsText(), PreservationMode.PreserveIdentity);
            foreach (var link in document.GetLinkedDocumentIds())
            {
                forkedSolution = forkedSolution.WithDocumentText(link, projectionBuffer.CurrentSnapshot.AsText(), PreservationMode.PreserveIdentity);
            }

            // Put it into a new workspace, and open it and its related documents
            // with the projection buffer as the text.
            var workspace = new IntellisenseTextBoxWorkspace(forkedSolution, document.Project, string.Empty);
            workspace.OpenDocument(workspace.ChangeSignatureDocumentId, originalContextBuffer.AsTextContainer());
            foreach (var link in document.GetLinkedDocumentIds())
            {
                workspace.OpenDocument(link, originalContextBuffer.AsTextContainer());
            }

            _editorAdaptersFactoryService.SetDataBuffer(vsTextLines, projectionBuffer);

            var result = new IntellisenseTextBoxViewModel[rolesCollections.Length];
            for (var i = 0; i < rolesCollections.Length; i++)
            {
                var roleSet = _textEditorFactoryService.CreateTextViewRoleSet(rolesCollections[i]);

                var vsTextView = _editorAdaptersFactoryService.CreateVsTextViewAdapter(serviceProvider, roleSet);

                vsTextView.Initialize(
                    vsTextLines,
                    IntPtr.Zero,
                    (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT,
                    s_InitViews);

                var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(vsTextView);
                wpfTextView.TextBuffer.ChangeContentType(contentType, null);

                result[i] = new IntellisenseTextBoxViewModel(vsTextView, wpfTextView);
            }

            return result;
        }
    }
}
