﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal partial class InvocationExpressionSignatureHelpProviderBase
    {
        internal virtual Task<(ImmutableArray<SignatureHelpItem> items, int? selectedItemIndex)> GetMethodGroupItemsAndSelectionAsync(
            ImmutableArray<IMethodSymbol> accessibleMethods,
            Document document,
            InvocationExpressionSyntax invocationExpression,
            SemanticModel semanticModel,
            SymbolInfo currentSymbol,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                (accessibleMethods.SelectAsArray(m => ConvertMethodGroupMethod(document, m, invocationExpression.SpanStart, semanticModel, cancellationToken)),
                 TryGetSelectedIndex(accessibleMethods, currentSymbol)));
        }

        private ImmutableArray<IMethodSymbol> GetAccessibleMethods(
            InvocationExpressionSyntax invocationExpression,
            SemanticModel semanticModel,
            ISymbol within,
            IEnumerable<IMethodSymbol> methodGroup,
            CancellationToken cancellationToken)
        {
            ITypeSymbol throughType = null;
            if (invocationExpression.Expression is MemberAccessExpressionSyntax)
            {
                var throughExpression = ((MemberAccessExpressionSyntax)invocationExpression.Expression).Expression;
                var throughSymbol = semanticModel.GetSymbolInfo(throughExpression, cancellationToken).GetAnySymbol();

                // if it is via a base expression "base.", we know the "throughType" is the base class but
                // we need to be able to tell between "base.M()" and "new Base().M()".
                // currently, Access check methods do not differentiate between them.
                // so handle "base." primary-expression here by nulling out "throughType"
                if (!(throughExpression is BaseExpressionSyntax))
                {
                    throughType = semanticModel.GetTypeInfo(throughExpression, cancellationToken).Type;
                }

                var includeInstance = !throughExpression.IsKind(SyntaxKind.IdentifierName) ||
                    semanticModel.LookupSymbols(throughExpression.SpanStart, name: throughSymbol.Name).Any(s => !(s is INamedTypeSymbol)) ||
                    (!(throughSymbol is INamespaceOrTypeSymbol) && semanticModel.LookupSymbols(throughExpression.SpanStart, container: throughSymbol.ContainingType).Any(s => !(s is INamedTypeSymbol)));

                var includeStatic = throughSymbol is INamedTypeSymbol ||
                    (throughExpression.IsKind(SyntaxKind.IdentifierName) &&
                    semanticModel.LookupNamespacesAndTypes(throughExpression.SpanStart, name: throughSymbol.Name).Any(t => Equals(t.GetSymbolType(), throughType)));

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

        private bool IsHiddenByOtherMethod(IMethodSymbol method, ISet<IMethodSymbol> methodSet)
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

        private bool IsHiddenBy(IMethodSymbol method1, IMethodSymbol method2)
        {
            // If they have the same parameter types and the same parameter names, then the 
            // constructed method is hidden by the unconstructed one.
            return method2.IsMoreSpecificThan(method1) == true;
        }
    }
}
