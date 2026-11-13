// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

internal abstract partial class InvocationExpressionSignatureHelpProviderBase
{
    internal virtual Task<(ImmutableArray<SignatureHelpItem> items, int? selectedItemIndex)> GetMethodGroupItemsAndSelectionAsync(
        ImmutableArray<IMethodSymbol> accessibleMethods,
        Document document,
        InvocationExpressionSyntax invocationExpression,
        SemanticModel semanticModel,
        SymbolInfo symbolInfo,
        IMethodSymbol? currentSymbol,
        CancellationToken cancellationToken)
    {
        var items = accessibleMethods.SelectAsArray(method => ConvertMethodGroupMethod(
            document, method, invocationExpression.SpanStart, semanticModel));
        var selectedItemIndex = TryGetSelectedIndex(accessibleMethods, currentSymbol);
        return Task.FromResult((items, selectedItemIndex));
    }

    private static ImmutableArray<IMethodSymbol> GetAccessibleMethods(
        InvocationExpressionSyntax invocationExpression,
        SemanticModel semanticModel,
        ISymbol within,
        IEnumerable<IMethodSymbol> methodGroup,
        CancellationToken cancellationToken)
    {
        methodGroup = InvocationMethodGroupFilter.FilterByStaticness(invocationExpression, semanticModel, methodGroup, out var throughType, cancellationToken);
        var accessibleMethods = methodGroup.Where(m => m.IsAccessibleWithin(within, throughType: throughType)).ToImmutableArrayOrEmpty();
        if (accessibleMethods.Length == 0)
        {
            return accessibleMethods;
        }

        var methodSet = accessibleMethods.ToSet();
        return accessibleMethods.Where(m => !IsHiddenByOtherMethod(m, methodSet)).ToImmutableArrayOrEmpty();
    }

    private static bool IsHiddenByOtherMethod(IMethodSymbol method, ISet<IMethodSymbol> methodSet)
    {
        foreach (var m in methodSet)
        {
            if (!Equals(m, method))
            {
                if (IsHiddenBy(method, m))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsHiddenBy(IMethodSymbol method1, IMethodSymbol method2)
    {
        // If they have the same parameter types and the same parameter names, then the 
        // constructed method is hidden by the unconstructed one.
        return method2.IsMoreSpecificThan(method1) == true;
    }
}
