// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

internal sealed partial class ObjectCreationExpressionSignatureHelpProvider
{
    public static SignatureHelpItem ConvertNormalTypeConstructor(
        IMethodSymbol constructor,
        int position,
        SemanticModel semanticModel,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        IDocumentationCommentFormattingService documentationCommentFormattingService)
    {
        var item = CreateItem(
            constructor, semanticModel, position,
            structuralTypeDisplayService,
            constructor.IsParams(),
            constructor.GetDocumentationPartsFactory(semanticModel, position, documentationCommentFormattingService),
            GetNormalTypePreambleParts(constructor, semanticModel, position),
            GetSeparatorParts(),
            GetNormalTypePostambleParts(),
            [.. constructor.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService))]);

        return item;
    }

    private static IList<SymbolDisplayPart> GetNormalTypePreambleParts(
        IMethodSymbol method,
        SemanticModel semanticModel,
        int position)
    {
        var result = new List<SymbolDisplayPart>();

        result.AddRange(method.ContainingType.ToMinimalDisplayParts(semanticModel, position));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));

        return result;
    }

    private static IList<SymbolDisplayPart> GetNormalTypePostambleParts()
        => [Punctuation(SyntaxKind.CloseParenToken)];
}
