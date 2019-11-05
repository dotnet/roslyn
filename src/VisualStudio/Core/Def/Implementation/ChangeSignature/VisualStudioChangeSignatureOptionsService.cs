// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
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
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
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
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly IVsEditorAdaptersFactoryService _vsEditorAdaptersFactoryService;
        private readonly IServiceProvider _originalServiceProvider;
        private readonly IContentType _contentType;
        private readonly OLE.Interop.IServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioChangeSignatureOptionsService(
            IClassificationFormatMapService classificationFormatMapService,
            ClassificationTypeMap classificationTypeMap,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            ITextEditorFactoryService textEditorFactoryService,
            IVsEditorAdaptersFactoryService vsEditorAdaptersFactoryService,
            IContentTypeRegistryService contentTypeRegistryService,
            SVsServiceProvider services)
        {
            _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("tooltip");
            _classificationTypeMap = classificationTypeMap;
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
            _vsEditorAdaptersFactoryService = vsEditorAdaptersFactoryService;
            _contentType = contentTypeRegistryService.GetContentType(ContentTypeNames.CSharpContentType);
            _originalServiceProvider = services;
            _serviceProvider = (OLE.Interop.IServiceProvider)services.GetService(typeof(OLE.Interop.IServiceProvider));
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

        public async System.Threading.Tasks.Task AttachToEditorAsync(Document document, CancellationToken cancellationToken)
        {
            var vsTextView = (await GetTextViewAsync(document, cancellationToken).ConfigureAwait(false)).Item1;
            var wpfTextView = _editorAdaptersFactoryService.GetWpfTextView(vsTextView);
            var target = new AddParameterDialogOleCommandTarget(wpfTextView, _editorAdaptersFactoryService, _originalServiceProvider);
            target.AttachToVsTextView();
        }

        private async Task<string> GetDocumentTextAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);

            return sourceText.ToString();
        }

        private async Task<(IVsTextView, IWpfTextViewHost)> GetTextViewAsync(Document document, CancellationToken cancellationToken)
        {
            var documentText = await GetDocumentTextAsync(document, cancellationToken).ConfigureAwait(false);

            var roleSet = _textEditorFactoryService.CreateTextViewRoleSet(
                PredefinedTextViewRoles.Document,
                PredefinedTextViewRoles.Editable,
                PredefinedTextViewRoles.Interactive);

            var textViewAdapter = _vsEditorAdaptersFactoryService.CreateVsTextViewAdapter(_serviceProvider, roleSet);
            var bufferAdapter = _vsEditorAdaptersFactoryService.CreateVsTextBufferAdapter(_serviceProvider, _contentType);
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

            var textViewHost = _vsEditorAdaptersFactoryService.GetWpfTextViewHost(textViewAdapter);

            return (textViewAdapter, textViewHost);
        }
    }
}
