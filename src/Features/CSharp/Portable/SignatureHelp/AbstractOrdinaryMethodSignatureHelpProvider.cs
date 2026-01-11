// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

internal abstract class AbstractOrdinaryMethodSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
{
    internal static SignatureHelpItem ConvertMethodGroupMethod(
        Document document,
        IMethodSymbol method,
        int position,
        SemanticModel semanticModel)
    {
        return ConvertMethodGroupMethod(document, method, position, semanticModel, descriptionParts: null);
    }

    internal static SignatureHelpItem ConvertMethodGroupMethod(
        Document document,
        IMethodSymbol method,
        int position,
        SemanticModel semanticModel,
        IList<SymbolDisplayPart>? descriptionParts)
    {
        var structuralTypeDisplayService = document.GetRequiredLanguageService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

        return CreateItemImpl(
            method, semanticModel, position,
            structuralTypeDisplayService,
            method.IsParams(),
            c => method.OriginalDefinition.GetDocumentationParts(semanticModel, position, documentationCommentFormattingService, c),
            GetMethodGroupPreambleParts(method, semanticModel, position),
            GetSeparatorParts(),
            GetMethodGroupPostambleParts(),
            [.. method.Parameters.Select(p => Convert(p, semanticModel, position, documentationCommentFormattingService))],
            descriptionParts: descriptionParts,
            static symbol => symbol is null ? null : SymbolDisplay.ToDisplayString(symbol, SymbolDisplayFormat.MinimallyQualifiedFormat));
    }

    private static IList<SymbolDisplayPart> GetMethodGroupPreambleParts(
        IMethodSymbol method,
        SemanticModel semanticModel,
        int position)
    {
        var result = new List<SymbolDisplayPart>();

        var awaitable = method.GetOriginalUnreducedDefinition().IsAwaitableNonDynamic(semanticModel, position);
        var extension = method.GetOriginalUnreducedDefinition().IsExtensionMethod();

        if (awaitable && extension)
        {
            result.Add(Punctuation(SyntaxKind.OpenParenToken));
            result.Add(Text(CSharpFeaturesResources.awaitable));
            result.Add(Punctuation(SyntaxKind.CommaToken));
            result.Add(Text(CSharpFeaturesResources.extension));
            result.Add(Punctuation(SyntaxKind.CloseParenToken));
            result.Add(Space());
        }
        else if (awaitable)
        {
            result.Add(Punctuation(SyntaxKind.OpenParenToken));
            result.Add(Text(CSharpFeaturesResources.awaitable));
            result.Add(Punctuation(SyntaxKind.CloseParenToken));
            result.Add(Space());
        }
        else if (extension)
        {
            result.Add(Punctuation(SyntaxKind.OpenParenToken));
            result.Add(Text(CSharpFeaturesResources.extension));
            result.Add(Punctuation(SyntaxKind.CloseParenToken));
            result.Add(Space());
        }

        result.AddRange(SymbolDisplay.ToMinimalDisplayParts(method, semanticModel, position, MinimallyQualifiedWithoutParametersFormat));
        result.Add(Punctuation(SyntaxKind.OpenParenToken));

        return result;
    }

    private static IList<SymbolDisplayPart> GetMethodGroupPostambleParts()
        => [Punctuation(SyntaxKind.CloseParenToken)];
}
