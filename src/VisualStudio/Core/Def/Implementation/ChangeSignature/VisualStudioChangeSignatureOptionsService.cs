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
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using ImportingConstructorAttribute = System.Composition.ImportingConstructorAttribute;

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

            var rolesCollection1 = new[] { PredefinedTextViewRoles.Editable, PredefinedTextViewRoles.Interactive,
                AddParameterTextViewRole, AddParameterTypeTextViewRole };
            var rolesCollection2 = new[] { PredefinedTextViewRoles.Editable, PredefinedTextViewRoles.Interactive,
                AddParameterTextViewRole, AddParameterNameTextViewRole };
            var rolesCollections = new[] { rolesCollection1, rolesCollection2 };

            var viewModels = await _intellisenseTextBoxViewModelFactory.CreateIntellisenseTextBoxViewModelsAsync(
                // TODO for VB there should be something like ", AS " and corresponding mapping should be opposite.
                document, _contentType, documentText.Insert(insertPosition, ", "),
                CreateTrackingSpans, rolesCollections, cancellationToken).ConfigureAwait(false);

            return new AddParameterDialog(viewModels[0], viewModels[1], document.Project.Solution.Workspace.Services.GetService<INotificationService>());

            ITrackingSpan[] CreateTrackingSpans(IProjectionSnapshot snapshot)
            {
                // Adjust the context point to ensure that the right information is in scope.
                // For example, we may need to move the point to the end of the last statement in a method body
                // in order to be able to access all local variables.
                // + 1 to support inserted comma
                var contextPoint = insertPosition + 1;

                // Get the previous span/text. We might have to insert another newline or something.
                var previousStatementSpan = snapshot.CreateTrackingSpanFromStartToIndex(contextPoint, SpanTrackingMode.EdgeNegative);

                // Get the appropriate ITrackingSpan for the window the user is typing in
                var mappedSpan1 = snapshot.CreateTrackingSpan(contextPoint, 0, SpanTrackingMode.EdgeExclusive);
                var spaceSpan = snapshot.CreateTrackingSpan(contextPoint, 1, SpanTrackingMode.EdgeExclusive);
                var mappedSpan2 = snapshot.CreateTrackingSpan(contextPoint + 1, 0, SpanTrackingMode.EdgeExclusive);

                // Build the tracking span that includes the rest of the file
                var restOfFileSpan = snapshot.CreateTrackingSpanFromIndexToEnd(contextPoint + 1, SpanTrackingMode.EdgePositive);
                return new ITrackingSpan[] { previousStatementSpan, mappedSpan1, spaceSpan, mappedSpan2, restOfFileSpan };
            }
        }
    }
}
