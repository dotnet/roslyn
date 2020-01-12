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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    [ExportWorkspaceService(typeof(IChangeSignatureOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioChangeSignatureOptionsService : IChangeSignatureOptionsService
    {
        public const string AddParameterTextViewRole = "AddParameter";
        public const string AddParameterTypeTextViewRole = "AddParameterType";
        public const string AddParameterNameTextViewRole = "AddParameterName";

        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IContentType _contentType;
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
            _contentType = contentTypeRegistryService.GetContentType(ContentTypeNames.CSharpContentType);
            _intellisenseTextBoxViewModelFactory = intellisenseTextBoxViewModelFactory;
        }

        public ChangeSignatureOptionsResult GetChangeSignatureOptions(
            ISymbol symbol,
            int insertPosition,
            ParameterConfiguration parameters,
            Document document)
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
                var viewModel = dialog.ViewModel;
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

            var rolesCollectionType = new[] { PredefinedTextViewRoles.Editable, PredefinedTextViewRoles.Interactive,
                AddParameterTextViewRole, AddParameterTypeTextViewRole };
            var rolesCollectionName = new[] { PredefinedTextViewRoles.Editable, PredefinedTextViewRoles.Interactive,
                AddParameterTextViewRole, AddParameterNameTextViewRole };

            var languageService = document.GetLanguageService<IChangeSignatureLanguageService>();
            var viewModels = await languageService.CreateViewModelsAsync(
                rolesCollectionType,
                rolesCollectionName,
                insertPosition,
                document,
                documentText,
                _contentType,
                _intellisenseTextBoxViewModelFactory,
                cancellationToken).ConfigureAwait(false);

            return new AddParameterDialog(viewModels[0], viewModels[1], document.Project.Solution.Workspace.Services.GetService<INotificationService>(), document);
        }
    }
}
