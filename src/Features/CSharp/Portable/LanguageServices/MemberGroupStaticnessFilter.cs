// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.LanguageService;

/// <summary>
/// Shared by signature help and quick info to remove members of a method group that cannot be invoked through the
/// receiver (or lack of receiver) used at the call site: instance methods through a type, static methods through an
/// instance, and instance methods from a static context.
/// </summary>
internal static class MemberGroupStaticnessFilter
{
    /// <param name="invokedExpression">The expression being invoked, e.g. <c>C.M</c> or <c>M</c> in <c>C.M()</c> / <c>M()</c>.</param>
    public static IEnumerable<TSymbol> Filter<TSymbol>(
        ExpressionSyntax invokedExpression,
        IEnumerable<TSymbol> memberGroup,
        SemanticModel semanticModel,
        CancellationToken cancellationToken) where TSymbol : ISymbol
    {
        if (invokedExpression is MemberAccessExpressionSyntax memberAccess)
        {
            var throughExpression = memberAccess.Expression;
            var throughSymbol = semanticModel.GetSymbolInfo(throughExpression, cancellationToken).GetAnySymbol();
            var throughType = throughExpression is BaseExpressionSyntax
                ? null
                : semanticModel.GetTypeInfo(throughExpression, cancellationToken).Type;

            // SyntaxKind.IdentifierName is for basic case, e.g. "MyClass.MyStaticMethod(...)"
            // SyntaxKind.SimpleMemberAccessExpression is for not imported types, e.g. "MyNamespace.MyClass.MyStaticMethod(...)"
            // SyntaxKind.PredefinedType is for built-in types, e.g. "string.Equals(...)"
            var includeInstance = throughExpression.Kind() is not (SyntaxKind.IdentifierName or SyntaxKind.SimpleMemberAccessExpression or SyntaxKind.PredefinedType) ||
                throughSymbol is not INamespaceOrTypeSymbol ||
                semanticModel.LookupSymbols(throughExpression.SpanStart, name: throughSymbol?.Name).Any(static s => s is not INamedTypeSymbol);

            var includeStatic = throughSymbol is INamedTypeSymbol ||
                (throughExpression.IsKind(SyntaxKind.IdentifierName) &&
                semanticModel.LookupNamespacesAndTypes(throughExpression.SpanStart, name: throughSymbol?.Name).Any(static (t, throughType) => object.Equals(t.GetSymbolType(), throughType), throughType));

            // Can't classify the receiver (invalid code); don't filter anything.
            if (!includeInstance && !includeStatic)
                return memberGroup;

            return memberGroup.Where(m => m is not IMethodSymbol || (m.IsStatic && includeStatic) || (!m.IsStatic && includeInstance));
        }

        if (invokedExpression is SimpleNameSyntax &&
            invokedExpression.Parent is not (MemberAccessExpressionSyntax or MemberBindingExpressionSyntax or QualifiedNameSyntax) &&
            invokedExpression.IsInStaticContext())
        {
            // We always need to include local functions regardless of whether they are static.
            return memberGroup.Where(static m => m is not IMethodSymbol method || method.IsStatic || method.MethodKind == MethodKind.LocalFunction);
        }

        return memberGroup;
    }
}
