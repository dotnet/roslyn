// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification.Classifiers;

internal abstract class AbstractNameSyntaxClassifier : AbstractSyntaxClassifier
{
    protected abstract bool IsParentAnAttribute(SyntaxNode node);

    protected ISymbol? TryGetSymbol(SyntaxNode node, SymbolInfo symbolInfo)
    {
        var symbol = symbolInfo.GetAnySymbol();

        // Classify a reference to an attribute constructor in an attribute location
        // as if we were classifying the attribute type itself.
        if (symbol.IsConstructor() && IsParentAnAttribute(node))
        {
            symbol = symbol.ContainingType;
        }

        return symbol;
    }

    protected static void TryClassifyStaticSymbol(
        ISymbol symbol,
        TextSpan span,
        SegmentedList<ClassifiedSpan> result)
    {
        if (IsStaticSymbol(symbol))
            result.Add(new ClassifiedSpan(span, ClassificationTypeNames.StaticSymbol));
    }

    protected static bool IsStaticSymbol(ISymbol symbol)
    {
        if (!symbol.IsStatic)
        {
            return false;
        }

        if (symbol.IsEnumMember())
        {
            // EnumMembers are not classified as static since there is no
            // instance equivalent of the concept and they have their own
            // classification type.
            return false;
        }

        if (symbol.IsNamespace())
        {
            // Namespace names are not classified as static since there is no
            // instance equivalent of the concept and they have their own
            // classification type.
            return false;
        }

        return true;
    }
}
