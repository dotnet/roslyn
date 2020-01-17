// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
                return new ChangeSignatureOptionsResult { IsCancelled = false, UpdatedSignature = new SignatureChange(parameters, viewModel.GetParameterConfiguration()), PreviewChanges = viewModel.PreviewChanges };
            }

            return new ChangeSignatureOptionsResult { IsCancelled = true };
        }

        public AddedParameter? GetAddedParameter(Document document, int insertPosition)
        {
            var dialog = CreateAddParameterDialog(document, insertPosition);
            if (dialog != null)
            {
                var result = dialog.ShowModal();

                if (result.HasValue && result.Value)
                {
                    var viewModel = dialog.ViewModel;
                    return new AddedParameter(
                            viewModel.TypeName,
                            viewModel.ParameterName,
                            viewModel.CallSiteValue);
                }
            }

            return null;
        }

        private AddParameterDialog? CreateAddParameterDialog(Document document, int insertPosition)
        {
            // TODO to be addressed in this PR
            // Should not create documentText here. Should move it later.
            var syntaxTree = document.GetSyntaxTreeAsync(CancellationToken.None).Result;
            if (syntaxTree == null)
            {
                return null;
            }

            var sourceText = syntaxTree.GetTextAsync(CancellationToken.None).Result;
            var documentText = sourceText.ToString();

            var rolesCollectionForTypeTextBox = new[] { PredefinedTextViewRoles.Editable, PredefinedTextViewRoles.Interactive,
                AddParameterTextViewRole, AddParameterTypeTextViewRole };
            var rolesCollectionForNameTextBox = new[] { PredefinedTextViewRoles.Editable, PredefinedTextViewRoles.Interactive,
                AddParameterTextViewRole, AddParameterNameTextViewRole };

            var languageService = document.GetRequiredLanguageService<IChangeSignatureLanguageService>();
            var viewModels = languageService.CreateViewModels(
                rolesCollectionForTypeTextBox,
                rolesCollectionForNameTextBox,
                insertPosition,
                document,
                documentText,
                _contentType,
                _intellisenseTextBoxViewModelFactory);

            return new AddParameterDialog(viewModels[0], viewModels[1], document.Project.Solution.Workspace.Services.GetService<INotificationService>(), document);
        }
    }
}
