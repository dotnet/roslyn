// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
        ITypeSymbol? throughType = null;
        if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var throughExpression = memberAccess.Expression;
            var throughSymbol = semanticModel.GetSymbolInfo(throughExpression, cancellationToken).GetAnySymbol();

            // if it is via a base expression "base.", we know the "throughType" is the base class but
            // we need to be able to tell between "base.M()" and "new Base().M()".
            // currently, Access check methods do not differentiate between them.
            // so handle "base." primary-expression here by nulling out "throughType"
            if (throughExpression is not BaseExpressionSyntax)
            {
                throughType = semanticModel.GetTypeInfo(throughExpression, cancellationToken).Type;
            }

            // SyntaxKind.IdentifierName is for basic case, e.g. "MyClass.MyStaticMethod(...)"
            // SyntaxKind.SimpleMemberAccessExpression is for not imported types, e.g. "MyNamespace.MyClass.MyStaticMethod(...)"
            // SyntaxKind.PredefinedType is for built-in types, e.g. "string.Equals(...)"
            var includeInstance = throughExpression.Kind() is not (SyntaxKind.IdentifierName or SyntaxKind.SimpleMemberAccessExpression or SyntaxKind.PredefinedType) ||
                semanticModel.LookupSymbols(throughExpression.SpanStart, name: throughSymbol?.Name).Any(static s => s is not INamedTypeSymbol) ||
                (throughSymbol is not INamespaceOrTypeSymbol && semanticModel.LookupSymbols(throughExpression.SpanStart, container: throughSymbol?.ContainingType).Any(static s => s is not INamedTypeSymbol));

            var includeStatic = throughSymbol is INamedTypeSymbol ||
                (throughExpression.IsKind(SyntaxKind.IdentifierName) &&
                semanticModel.LookupNamespacesAndTypes(throughExpression.SpanStart, name: throughSymbol?.Name).Any(static (t, throughType) => Equals(t.GetSymbolType(), throughType), throughType));

            Contract.ThrowIfFalse(includeInstance || includeStatic);
            methodGroup = methodGroup.Where(m => (m.IsStatic && includeStatic) || (!m.IsStatic && includeInstance));
        }
        else if (invocationExpression.Expression is SimpleNameSyntax &&
            invocationExpression.IsInStaticContext())
        {
            // We always need to include local functions regardless of whether they are static.
            methodGroup = methodGroup.Where(m => m.IsStatic || m is IMethodSymbol { MethodKind: MethodKind.LocalFunction });
        }

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
