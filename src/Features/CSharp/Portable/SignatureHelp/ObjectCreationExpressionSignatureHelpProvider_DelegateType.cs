// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

internal sealed partial class ObjectCreationExpressionSignatureHelpProvider
{
    private static ImmutableArray<SignatureHelpItem> ConvertDelegateTypeConstructor(
        BaseObjectCreationExpressionSyntax objectCreationExpression,
        IMethodSymbol invokeMethod,
        SemanticModel semanticModel,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        int position)
    {
        var item = CreateItem(
            invokeMethod, semanticModel,
            objectCreationExpression.SpanStart,
            structuralTypeDisplayService,
            isVariadic: false,
            documentationFactory: null,
            prefixParts: GetDelegateTypePreambleParts(invokeMethod, semanticModel, position),
            separatorParts: GetSeparatorParts(),
            suffixParts: GetDelegateTypePostambleParts(),
            parameters: GetDelegateTypeParameters(invokeMethod, semanticModel, position));

        return [item];
    }

    private static ImmutableArray<SymbolDisplayPart> GetDelegateTypePreambleParts(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
        => [
            .. invokeMethod.ContainingType.ToMinimalDisplayParts(semanticModel, position),
            Punctuation(SyntaxKind.OpenParenToken),
        ];

    private static ImmutableArray<SignatureHelpSymbolParameter> GetDelegateTypeParameters(IMethodSymbol invokeMethod, SemanticModel semanticModel, int position)
    {
        const string TargetName = "target";

        using var _ = ArrayBuilder<SymbolDisplayPart>.GetInstance(out var parts);
        parts.AddRange(invokeMethod.ReturnType.ToMinimalDisplayParts(semanticModel, position));
        parts.Add(Space());
        parts.Add(Punctuation(SyntaxKind.OpenParenToken));

        var first = true;
        foreach (var parameter in invokeMethod.Parameters)
        {
            if (!first)
            {
                parts.Add(Punctuation(SyntaxKind.CommaToken));
                parts.Add(Space());
            }

            first = false;
            parts.AddRange(parameter.Type.ToMinimalDisplayParts(semanticModel, position));
        }

        parts.Add(Punctuation(SyntaxKind.CloseParenToken));
        parts.Add(Space());
        parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ParameterName, null, TargetName));

        return [new SignatureHelpSymbolParameter(
            TargetName,
            isOptional: false,
            documentationFactory: null,
            displayParts: parts.ToImmutableAndClear())];
    }

    private static ImmutableArray<SymbolDisplayPart> GetDelegateTypePostambleParts()
        => [Punctuation(SyntaxKind.CloseParenToken)];
}
