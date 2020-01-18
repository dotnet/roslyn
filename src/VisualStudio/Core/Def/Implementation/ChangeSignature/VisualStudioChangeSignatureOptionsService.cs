// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    [ExportWorkspaceService(typeof(IChangeSignatureOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioChangeSignatureOptionsService : IChangeSignatureOptionsService
    {
        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly IntellisenseTextBoxViewModelFactory _intellisenseTextBoxViewModelFactory;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioChangeSignatureOptionsService(
            IClassificationFormatMapService classificationFormatMapService,
            ClassificationTypeMap classificationTypeMap,
            IContentTypeRegistryService contentTypeRegistryService,
            IntellisenseTextBoxViewModelFactory intellisenseTextBoxViewModelFactory)
        {
            _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("tooltip");
            _classificationTypeMap = classificationTypeMap;
            _contentTypeRegistryService = contentTypeRegistryService;
            _intellisenseTextBoxViewModelFactory = intellisenseTextBoxViewModelFactory;
        }

        public ChangeSignatureOptionsResult? GetChangeSignatureOptions(
            Document document,
            int insertPosition,
            ISymbol symbol,
            ParameterConfiguration parameters)
        {
            var viewModel = new ChangeSignatureDialogViewModel(
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
                return new ChangeSignatureOptionsResult(new SignatureChange(parameters, viewModel.GetParameterConfiguration()), previewChanges: viewModel.PreviewChanges);
            }

            return null;
        }

        public AddedParameter? GetAddedParameter(Document document, int insertPosition)
        {
            var languageService = document.GetRequiredLanguageService<IChangeSignatureViewModelFactoryService>();
            var viewModelsCreationTask = languageService.CreateViewModelsAsync(
                _contentTypeRegistryService, _intellisenseTextBoxViewModelFactory, document, insertPosition);

            viewModelsCreationTask.Start();

            var dialog = new AddParameterDialog(viewModelsCreationTask, document.Project.Solution.Workspace.Services.GetService<INotificationService>(), document);
            var result = dialog.ShowModal();

            if (result.HasValue && result.Value)
            {
                var viewModel = dialog.ViewModel;
                return new AddedParameter(
                        viewModel.TypeName,
                        viewModel.ParameterName,
                        viewModel.CallSiteValue);
            }

            return null;
        }
    }
}
