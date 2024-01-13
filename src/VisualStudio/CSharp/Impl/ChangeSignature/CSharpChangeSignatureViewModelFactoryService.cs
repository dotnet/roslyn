// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature.ChangeSignatureDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ChangeSignature
{
    [ExportLanguageService(typeof(IChangeSignatureViewModelFactoryService), LanguageNames.CSharp), Shared]
    internal class CSharpChangeSignatureViewModelFactoryService : ChangeSignatureViewModelFactoryService
    {
        private static readonly CSharpParseOptions s_langVersionLatestParseOptions = new(LanguageVersion.Preview);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpChangeSignatureViewModelFactoryService()
        {
        }

        public override SymbolDisplayPart[] GeneratePreviewDisplayParts(AddedParameterViewModel addedParameterViewModel)
        {
            var parts = new List<SymbolDisplayPart>();

            // TO-DO: We need to add proper colorization for added parameters:
            // https://github.com/dotnet/roslyn/issues/47986
            var isPredefinedType = SyntaxFactory.ParseExpression(addedParameterViewModel.Type).Kind() == SyntaxKind.PredefinedType;
            var typePartKind = isPredefinedType ? SymbolDisplayPartKind.Keyword : SymbolDisplayPartKind.ClassName;

            parts.Add(new SymbolDisplayPart(typePartKind, null, addedParameterViewModel.Type));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " "));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, null, addedParameterViewModel.ParameterName));

            if (!string.IsNullOrWhiteSpace(addedParameterViewModel.Default))
            {
                parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " "));
                parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "="));
                parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " "));
                parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, addedParameterViewModel.Default));
            }

            return parts.ToArray();
        }

        // Use LangVersion Preview to ensure that all types parse correctly. If the user types in a type only available
        // in a preview version, they'll get a diagnostic after everything is generated
        public override bool IsTypeNameValid(string typeName) => !SyntaxFactory.ParseTypeName(typeName, options: s_langVersionLatestParseOptions).ContainsDiagnostics;

        public override SyntaxNode GetTypeNode(string typeName) => SyntaxFactory.ParseTypeName(typeName);
    }
}
