// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature.ChangeSignatureDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ChangeSignature
{
    [ExportLanguageService(typeof(IChangeSignatureLanguageService), LanguageNames.CSharp), Shared]
    internal class CSharpChangeSignatureLanguageService : ChangeSignatureLanguageService
    {
        public override async Task<IntellisenseTextBoxViewModel[]> CreateViewModelsAsync(
            string[] rolesCollectionType,
            string[] rolesCollectionName,
            int insertPosition,
            Document document,
            string documentText,
            IContentType contentType,
            IntellisenseTextBoxViewModelFactory intellisenseTextBoxViewModelFactory,
            CancellationToken cancellationToken)
        {
            var rolesCollections = new[] { rolesCollectionType, rolesCollectionName };

            return await intellisenseTextBoxViewModelFactory.CreateIntellisenseTextBoxViewModelsAsync(
               document, contentType, documentText.Insert(insertPosition, ", "),
               CreateTrackingSpans, rolesCollections, cancellationToken).ConfigureAwait(false);

            ITrackingSpan[] CreateTrackingSpans(ITextSnapshot snapshot)
            {
                // Adjust the context point to ensure that the right information is in scope.
                // For example, we may need to move the point to the end of the last statement in a method body
                // in order to be able to access all local variables.
                // + 1 to support inserted comma
                return CreateTrackingSpansHelper(snapshot, contextPoint: insertPosition + 1, spaceBetweenTypeAndName: 1);
            }
        }

        public override void GeneratePreviewDisplayParts(AddedParameterViewModel addedParameterViewModel, List<SymbolDisplayPart> displayParts)
        {
            displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, addedParameterViewModel.Type));
            displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " "));
            displayParts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, null, addedParameterViewModel.Parameter)); ;
        }

        public override bool IsTypeNameValid(string typeName) => !SyntaxFactory.ParseTypeName(typeName).ContainsDiagnostics;
    }
}
