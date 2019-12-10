// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using ImportingConstructorAttribute = System.Composition.ImportingConstructorAttribute;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    [ExportWorkspaceService(typeof(IChangeSignatureOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioChangeSignatureOptionsService : IChangeSignatureOptionsService
    {
        public const string AddParameterTextViewRole = "AddParameter";

        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IContentType _contentType;
        private readonly OLE.Interop.IServiceProvider _serviceProvider;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioChangeSignatureOptionsService(
            IClassificationFormatMapService classificationFormatMapService,
            ClassificationTypeMap classificationTypeMap,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            SVsServiceProvider services)
        {
            _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("tooltip");
            _classificationTypeMap = classificationTypeMap;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _contentType = contentTypeRegistryService.GetContentType(ContentTypeNames.CSharpContentType);
            _serviceProvider = (OLE.Interop.IServiceProvider)services.GetService(typeof(OLE.Interop.IServiceProvider));
            _projectionBufferFactoryService = projectionBufferFactoryService;
        }

        public ChangeSignatureOptionsResult GetChangeSignatureOptions(
            ISymbol symbol,
            int insertPosition,
            ParameterConfiguration parameters,
            Document document,
            INotificationService notificationService)
        {
            var viewModel = new ChangeSignatureDialogViewModel(
                notificationService,
                parameters,
                symbol,
                document,
                insertPosition,
                _classificationFormatMap,
                _classificationTypeMap);

            var dialog = new ChangeSignatureDialog(viewModel);
            var result = dialog.ShowModal();

            if (result.HasValue && result.Value)
            {
                return new ChangeSignatureOptionsResult { IsCancelled = false, UpdatedSignature = new SignatureChange(parameters, viewModel.GetParameterConfiguration()), PreviewChanges = viewModel.PreviewChanges };
            }

            return new ChangeSignatureOptionsResult { IsCancelled = true };
        }

        public AddedParameterResult GetAddedParameter(Document document, int insertPosition)
        {
            var dialog = CreateAddParameterDialogAsync(document, insertPosition, CancellationToken.None).Result;
            var result = dialog.ShowModal();

            if (result.HasValue && result.Value)
            {
                AddParameterDialogViewModel viewModel = dialog.ViewModel;
                return new AddedParameterResult
                {
                    IsCancelled = false,
                    AddedParameter = new AddedParameter(
                        viewModel.TypeName,
                        viewModel.ParameterName,
                        viewModel.CallsiteValue)
                };
            }
            else
            {
                return new AddedParameterResult { IsCancelled = true };
            }
        }

        private async Task<AddParameterDialog> CreateAddParameterDialogAsync(
            Document document, int insertPosition, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var documentText = sourceText.ToString();

            IVsTextLines vsTextLines = _editorAdaptersFactoryService.CreateVsTextBufferAdapter(_serviceProvider, _contentType) as IVsTextLines;
            vsTextLines.InitializeContent(documentText.Insert(insertPosition, ","), documentText.Length);

            var originalContextBuffer = _editorAdaptersFactoryService.GetDataBuffer(vsTextLines);
            // Get the workspace, and from there, the solution and document containing this buffer.
            // If there's an ExternalSource, we won't get a document. Give up in that case.
            var solution = document.Project.Solution;

            // Wrap the original ContextBuffer in a projection buffer that we can make read-only
            var contextBuffer = _projectionBufferFactoryService.CreateProjectionBuffer(null,
                new object[] { originalContextBuffer.CurrentSnapshot.CreateFullTrackingSpan(SpanTrackingMode.EdgeInclusive) }, ProjectionBufferOptions.None, _contentType);

            // Make projection readonly so we can't edit it by mistake.
            using (var regionEdit = contextBuffer.CreateReadOnlyRegionEdit())
            {
                regionEdit.CreateReadOnlyRegion(new Span(0, contextBuffer.CurrentSnapshot.Length), SpanTrackingMode.EdgeInclusive, EdgeInsertionMode.Deny);
                regionEdit.Apply();
            }

            // Adjust the context point to ensure that the right information is in scope.
            // For example, we may need to move the point to the end of the last statement in a method body
            // in order to be able to access all local variables.
            // + 1 to support inserted comma
            var contextPoint = insertPosition + 1;

            // Get the previous span/text. We might have to insert another newline or something.
            var previousStatementSpan = contextBuffer.CurrentSnapshot.CreateTrackingSpanFromStartToIndex(contextPoint, SpanTrackingMode.EdgeNegative);

            // Get the appropriate ITrackingSpan for the window the user is typing in
            var mappedSpan = contextBuffer.CurrentSnapshot.CreateTrackingSpan(contextPoint, 0, SpanTrackingMode.EdgeExclusive);

            // Build the tracking span that includes the rest of the file
            var restOfFileSpan = contextBuffer.CurrentSnapshot.CreateTrackingSpanFromIndexToEnd(contextPoint, SpanTrackingMode.EdgePositive);

            // Put it all into a projection buffer
            var projectionBuffer = _projectionBufferFactoryService.CreateProjectionBuffer(null,
                new object[] { previousStatementSpan, mappedSpan, restOfFileSpan }, ProjectionBufferOptions.None, _contentType);

            // Fork the solution using this new primary buffer for the document and all of its linked documents.
            var forkedSolution = solution.WithDocumentText(document.Id, projectionBuffer.CurrentSnapshot.AsText(), PreservationMode.PreserveIdentity);
            foreach (var link in document.GetLinkedDocumentIds())
            {
                forkedSolution = forkedSolution.WithDocumentText(link, projectionBuffer.CurrentSnapshot.AsText(), PreservationMode.PreserveIdentity);
            }

            // Put it into a new workspace, and open it and its related documents
            // with the projection buffer as the text.
            var workspace = new ChangeSignatureWorkspace(forkedSolution, document.Project);
            workspace.OpenDocument(workspace.ChangeSignatureDocumentId, originalContextBuffer.AsTextContainer());
            foreach (var link in document.GetLinkedDocumentIds())
            {
                workspace.OpenDocument(link, originalContextBuffer.AsTextContainer());
            }

            // Start getting the compilation so the PartialSolution will be ready when the user starts typing in the window
            await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var roleSet = _textEditorFactoryService.CreateTextViewRoleSet(
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Interactive,
                AddParameterTextViewRole);

            var vsTextView = _editorAdaptersFactoryService.CreateVsTextViewAdapter(_serviceProvider, roleSet);

            var initView = new[] {
                new INITVIEW()
                {
                    fSelectionMargin = 0,
                    fWidgetMargin = 0,
                    fDragDropMove = 0,
                    IndentStyle = vsIndentStyle.vsIndentStyleNone
                }
            };

            _editorAdaptersFactoryService.SetDataBuffer(vsTextLines, projectionBuffer);

            vsTextView.Initialize(
                vsTextLines,
                IntPtr.Zero,
                (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT,
                initView);

            IWpfTextView wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(vsTextView);
            wpfTextView.TextBuffer.ChangeContentType(_contentType, null);

            return new AddParameterDialog(vsTextLines, vsTextView, wpfTextView);
        }
    }
}
