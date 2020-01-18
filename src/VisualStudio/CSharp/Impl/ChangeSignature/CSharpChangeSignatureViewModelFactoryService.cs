// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;
using Microsoft.VisualStudio.Text;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature.ChangeSignatureDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ChangeSignature
{
    [ExportLanguageService(typeof(IChangeSignatureViewModelFactoryService), LanguageNames.CSharp), Shared]
    internal class CSharpChangeSignatureViewModelFactoryService : ChangeSignatureViewModelFactoryService
    {
        // Adjust the context point to ensure that the right information is in scope.
        // For example, we may need to move the point to the end of the last statement in a method body
        // in order to be able to access all local variables.
        // + 1 to support inserted comma
        protected override ITrackingSpan[] CreateSpansMethod(ITextSnapshot textSnapshot, int insertPosition)
            => CreateTrackingSpansHelper(textSnapshot, contextPoint: insertPosition + 1, spaceBetweenTypeAndName: 1);

        protected override string TextToInsert => ", ";

        protected override string ContentTypeName => ContentTypeNames.CSharpContentType;

        public override SymbolDisplayPart[] GeneratePreviewDisplayParts(AddedParameterViewModel addedParameterViewModel)
            => new[] {
                new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, addedParameterViewModel.Type),
                new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " "),
                new SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, null, addedParameterViewModel.ParameterName)};

        public override bool IsTypeNameValid(string typeName) => !SyntaxFactory.ParseTypeName(typeName).ContainsDiagnostics;
    }
}
