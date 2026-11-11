// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.LanguageService;

internal static class InvocationMethodGroupFilter
{
    /// <summary>
    /// Removes methods from <paramref name="methodGroup"/> that cannot be invoked through the receiver (or lack of
    /// receiver) of <paramref name="invocationExpression"/>.
    /// </summary>
    public static IEnumerable<IMethodSymbol> FilterByStaticness(
        InvocationExpressionSyntax invocationExpression,
        SemanticModel semanticModel,
        IEnumerable<IMethodSymbol> methodGroup,
        out ITypeSymbol? throughType,
        CancellationToken cancellationToken)
    {
        throughType = null;
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
                throughSymbol is not INamespaceOrTypeSymbol ||
                semanticModel.LookupSymbols(throughExpression.SpanStart, name: throughSymbol?.Name).Any(static s => s is not INamedTypeSymbol);

            var includeStatic = throughSymbol is INamedTypeSymbol ||
                (throughExpression.IsKind(SyntaxKind.IdentifierName) &&
                semanticModel.LookupNamespacesAndTypes(throughExpression.SpanStart, name: throughSymbol?.Name).Any(static (t, throughType) => Equals(t.GetSymbolType(), throughType), throughType));

            Contract.ThrowIfFalse(includeInstance || includeStatic);
            return methodGroup.Where(m => (m.IsStatic && includeStatic) || (!m.IsStatic && includeInstance));
        }
        else if (invocationExpression.Expression is SimpleNameSyntax &&
            invocationExpression.IsInStaticContext())
        {
            // We always need to include local functions regardless of whether they are static.
            return methodGroup.Where(m => m.IsStatic || m is IMethodSymbol { MethodKind: MethodKind.LocalFunction });
        }

        return methodGroup;
    }
}
