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
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using ImportingConstructorAttribute = System.Composition.ImportingConstructorAttribute;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    [ExportWorkspaceService(typeof(IChangeSignatureOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioChangeSignatureOptionsService : IChangeSignatureOptionsService
    {
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IServiceProvider _originalServiceProvider;
        private readonly IContentType _contentType;
        private readonly OLE.Interop.IServiceProvider _serviceProvider;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private readonly IEditorCommandHandlerServiceFactory _editorCommandHandlerServiceFactory;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioChangeSignatureOptionsService(
            IClassificationFormatMapService classificationFormatMapService,
            ClassificationTypeMap classificationTypeMap,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            ITextBufferFactoryService textBufferFactoryService,
            IEditorCommandHandlerServiceFactory editorCommandHandlerServiceFactory,
            SVsServiceProvider services)
        {
            _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("tooltip");
            _classificationTypeMap = classificationTypeMap;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _contentType = contentTypeRegistryService.GetContentType(ContentTypeNames.CSharpContentType);
            _originalServiceProvider = services;
            _editorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory;
            _serviceProvider = (OLE.Interop.IServiceProvider)services.GetService(typeof(OLE.Interop.IServiceProvider));
            _textBufferFactoryService = textBufferFactoryService;
        }

        public ChangeSignatureOptionsResult GetChangeSignatureOptions(
            ISymbol symbol,
            ParameterConfiguration parameters,
            Document document,
            INotificationService notificationService)
        {
            var viewModel = new ChangeSignatureDialogViewModel(
                notificationService,
                parameters,
                symbol,
                document,
                _classificationFormatMap,
                _classificationTypeMap);

            var dialog = new ChangeSignatureDialog(viewModel);
            var result = dialog.ShowModal();

            if (result.HasValue && result.Value)
            {
                return new ChangeSignatureOptionsResult { IsCancelled = false, UpdatedSignature = new SignatureChange(parameters, viewModel.GetParameterConfiguration()), PreviewChanges = viewModel.PreviewChanges };
            }
            else
            {
                return new ChangeSignatureOptionsResult { IsCancelled = true };
            }
        }

        public AddedParameterResult GetAddedParameter(Document document)
        {
            // TODO async?
            var tuple = GetTextViewAsync(document, CancellationToken.None).Result;
            var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(tuple.Item1);
            var viewModel = new AddParameterDialogViewModel();
            var dialog = new AddParameterDialog(
                viewModel,
                _textEditorFactoryService,
                _textBufferFactoryService,
                _editorCommandHandlerServiceFactory,
                _editorOperationsFactoryService,
                _contentType);

            var result = dialog.ShowModal();

            if (result.HasValue && result.Value)
            {
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

        private async Task<string> GetDocumentTextAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return sourceText.ToString();
        }

        public async Task<(IVsTextView, IWpfTextViewHost)> GetTextViewAsync(Document document, CancellationToken cancellationToken)
        {
            var documentText = await GetDocumentTextAsync(document, cancellationToken).ConfigureAwait(false);

            var roleSet = _textEditorFactoryService.CreateTextViewRoleSet(
                PredefinedTextViewRoles.Document,
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Interactive);

            var textViewAdapter = _editorAdaptersFactoryService.CreateVsTextViewAdapter(_serviceProvider, roleSet);
            var bufferAdapter = _editorAdaptersFactoryService.CreateVsTextBufferAdapter(_serviceProvider, _contentType);
            bufferAdapter.InitializeContent(documentText, documentText.Length);

            //     var textBuffer = _vsEditorAdaptersFactoryService.GetDataBuffer(bufferAdapter);
            //     document.Project.Solution.Workspace.OnDocumentOpened(document.Id, textBuffer.AsTextContainer());

            var initView = new[] {
                new INITVIEW()
                {
                    fSelectionMargin = 0,
                    fWidgetMargin = 0,
                    fDragDropMove = 0,
                    IndentStyle = vsIndentStyle.vsIndentStyleNone
                }
            };

            textViewAdapter.Initialize(
                bufferAdapter as IVsTextLines,
                IntPtr.Zero,
                (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT,
                initView);

            var textViewHost = _editorAdaptersFactoryService.GetWpfTextViewHost(textViewAdapter);

            return (textViewAdapter, textViewHost);
        }
    }
}
