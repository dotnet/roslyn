// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Simplification;

internal static class SimplificationHelpers
{
    public static readonly SyntaxAnnotation DoNotSimplifyAnnotation = new();
    public static readonly SyntaxAnnotation SimplifyModuleNameAnnotation = new();

    public static TNode CopyAnnotations<TNode>(SyntaxNode from, TNode to) where TNode : SyntaxNode
    {
        // Because we are removing a node that may have annotations (i.e. formatting), we need
        // to copy those annotations to the new node. However, we can only copy all annotations
        // which will mean that the new node will include a ParenthesesSimplification annotation,
        // even if didn't have one before. That results in potentially removing parentheses that
        // weren't annotated by the user. To address this, we add *another* annotation to indicate
        // that the new node shouldn't be simplified. This is to work around the
        // fact that there is no way to remove an annotation from a node in the current API. If
        // that gets added, we can clean this up.

        var dontSimplifyResult = !to.HasAnnotation(Simplifier.Annotation);

        to = from.CopyAnnotationsTo(to);

        if (dontSimplifyResult)
        {
            to = to.WithAdditionalAnnotations(DoNotSimplifyAnnotation);
        }

        return to;
    }

    public static SyntaxToken CopyAnnotations(SyntaxToken from, SyntaxToken to)
    {
        // Because we are removing a node that may have annotations (i.e. formatting), we need
        // to copy those annotations to the new node. However, we can only copy all annotations
        // which will mean that the new node will include a ParenthesesSimplification annotation,
        // even if didn't have one before. That results in potentially removing parentheses that
        // weren't annotated by the user. To address this, we add *another* annotation to indicate
        // that the new node shouldn't be simplified. This is to work around the
        // fact that there is no way to remove an annotation from a node in the current API. If
        // that gets added, we can clean this up.

        var dontSimplifyResult = !to.HasAnnotation(Simplifier.Annotation);

        to = from.CopyAnnotationsTo(to);

        if (dontSimplifyResult)
        {
            to = to.WithAdditionalAnnotations(DoNotSimplifyAnnotation);
        }

        return to;
    }

    public static ISymbol? GetOriginalSymbolInfo(SemanticModel semanticModel, SyntaxNode expression)
    {
        Contract.ThrowIfNull(expression);
        var annotation1 = expression.GetAnnotations(SymbolAnnotation.Kind).FirstOrDefault();
        if (annotation1 != null)
        {
            var typeSymbol = SymbolAnnotation.GetSymbol(annotation1, semanticModel.Compilation);
            if (IsValidSymbolInfo(typeSymbol))
                return typeSymbol;
        }

        var annotation2 = expression.GetAnnotations(SpecialTypeAnnotation.Kind).FirstOrDefault();
        if (annotation2 != null)
        {
            var specialType = SpecialTypeAnnotation.GetSpecialType(annotation2);
            if (specialType != SpecialType.None)
            {
                var typeSymbol = semanticModel.Compilation.GetSpecialType(specialType);
                if (IsValidSymbolInfo(typeSymbol))
                    return typeSymbol;
            }
        }

        var symbolInfo = semanticModel.GetSymbolInfo(expression);
        if (!IsValidSymbolInfo(symbolInfo.Symbol))
            return null;

        return symbolInfo.Symbol;
    }

    public static bool IsValidSymbolInfo([NotNullWhen(true)] ISymbol? symbol)
    {
        // name bound to only one symbol is valid
        return symbol is not null and not IErrorTypeSymbol;
    }

    public static bool IsNamespaceOrTypeOrThisParameter(SyntaxNode expression, SemanticModel semanticModel)
    {
        var expressionInfo = semanticModel.GetSymbolInfo(expression);
        if (IsValidSymbolInfo(expressionInfo.Symbol))
        {
            if (expressionInfo.Symbol is INamespaceOrTypeSymbol)
                return true;

            if (expressionInfo.Symbol.IsThisParameter())
                return true;
        }

        return false;
    }

    internal static bool ShouldSimplifyThisOrMeMemberAccessExpression(SimplifierOptions options, ISymbol symbol)
    {
        // If we're accessing a static member off of this/me then we should always consider this
        // simplifiable.  Note: in C# this isn't even legal to access a static off of `this`,
        // but in VB it is legal to access a static off of `me`.
        if (symbol.IsStatic)
            return true;

        return options.TryGetQualifyMemberAccessOption(symbol.Kind, out var symbolOptions) && !symbolOptions.Value;
    }
}
