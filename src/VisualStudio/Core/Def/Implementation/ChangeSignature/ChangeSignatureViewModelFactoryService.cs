// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal abstract class ChangeSignatureViewModelFactoryService : IChangeSignatureViewModelFactoryService
    {
        public const string AddParameterTextViewRole = "AddParameter";
        public const string AddParameterTypeTextViewRole = "AddParameterType";
        public const string AddParameterNameTextViewRole = "AddParameterName";

        private static string[] rolesCollectionForTypeTextBox = new[] { PredefinedTextViewRoles.Editable, PredefinedTextViewRoles.Interactive,
                AddParameterTextViewRole, AddParameterTypeTextViewRole };

        private static string[] rolesCollectionForNameTextBox = new[] { PredefinedTextViewRoles.Editable, PredefinedTextViewRoles.Interactive,
                AddParameterTextViewRole, AddParameterNameTextViewRole };

        private static string[][] rolesCollections = new[] { rolesCollectionForTypeTextBox, rolesCollectionForNameTextBox };

        public async Task<ChangeSignatureIntellisenseTextBoxesViewModel?> CreateViewModelsAsync(
            IContentTypeRegistryService contentTypeRegistryService,
            IntellisenseTextBoxViewModelFactory intellisenseTextBoxViewModelFactory,
            Document document,
            int insertPosition)
        {
            var viewModels = await intellisenseTextBoxViewModelFactory.CreateIntellisenseTextBoxViewModelsAsync(
                document, contentTypeRegistryService.GetContentType(ContentTypeName), insertPosition, TextToInsert, CreateSpansMethod, rolesCollections).ConfigureAwait(false);

            if (viewModels == null)
            {
                return null;
            }

            // C# and VB have opposite orderings for name and type
            if (document.Project.Language.Equals(LanguageNames.CSharp))
            {
                return new ChangeSignatureIntellisenseTextBoxesViewModel(viewModels[0], viewModels[1]);
            }

            return new ChangeSignatureIntellisenseTextBoxesViewModel(viewModels[1], viewModels[0]);

        }

        public abstract SymbolDisplayPart[] GeneratePreviewDisplayParts(
            ChangeSignatureDialogViewModel.AddedParameterViewModel addedParameterViewModel);

        public abstract bool IsTypeNameValid(string typeName);

        protected abstract ITrackingSpan[] CreateSpansMethod(ITextSnapshot textSnapshot, int insertPosition);

        protected abstract string TextToInsert { get; }

        protected abstract string ContentTypeName { get; }

        protected ITrackingSpan[] CreateTrackingSpansHelper(ITextSnapshot snapshot, int contextPoint, int spaceBetweenTypeAndName)
        {
            // Get the previous span/text.
            var previousStatementSpan = snapshot.CreateTrackingSpan(Span.FromBounds(0, contextPoint), SpanTrackingMode.EdgeNegative);

            // Get the appropriate ITrackingSpan for the window the user is typing in.
            // mappedSpan1 is the 'Type' field for C# and 'Name' for VB.
            var mappedSpan1 = snapshot.CreateTrackingSpan(contextPoint, 0, SpanTrackingMode.EdgeExclusive);

            // Space could be either ' ' for C# or '] AS ' for VB
            var spaceSpan = snapshot.CreateTrackingSpan(contextPoint, spaceBetweenTypeAndName, SpanTrackingMode.EdgeExclusive);

            // mappedSpan2 is the 'Name' field for C# and 'Type' for VB.
            var mappedSpan2 = snapshot.CreateTrackingSpan(contextPoint + spaceBetweenTypeAndName, 0, SpanTrackingMode.EdgeExclusive);

            // Build the tracking span that includes the rest of the file.
            var restOfFileSpan = snapshot.CreateTrackingSpan(Span.FromBounds(contextPoint + spaceBetweenTypeAndName, snapshot.Length), SpanTrackingMode.EdgePositive);

            // This array as a whole should encompass the span of the entire file.
            return new ITrackingSpan[] { previousStatementSpan, mappedSpan1, spaceSpan, mappedSpan2, restOfFileSpan };
        }
    }
}
