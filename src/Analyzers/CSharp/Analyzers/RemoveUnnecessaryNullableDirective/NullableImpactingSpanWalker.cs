// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.RemoveUnnecessaryNullableDirective;

internal sealed class NullableImpactingSpanWalker(
    SemanticModel semanticModel,
    int positionOfFirstReducingNullableDirective,
    TextSpanIntervalTree? ignoredSpans,
    CancellationToken cancellationToken) : CSharpSyntaxWalker(SyntaxWalkerDepth.StructuredTrivia), IDisposable
{
    private readonly SemanticModel _semanticModel = semanticModel;
    private readonly int _positionOfFirstReducingNullableDirective = positionOfFirstReducingNullableDirective;
    private readonly TextSpanIntervalTree? _ignoredSpans = ignoredSpans;
    private readonly CancellationToken _cancellationToken = cancellationToken;

    private ImmutableArray<TextSpan>.Builder? _spans;

    public bool HasSpans => _spans?.Count > 0;

    public ImmutableArray<TextSpan> Spans => _spans?.ToImmutable() ?? [];

    public ImmutableArray<TextSpan>.Builder SpansBuilder
    {
        get
        {
            if (_spans is null)
                Interlocked.CompareExchange(ref _spans, ImmutableArray.CreateBuilder<TextSpan>(), null);

            return _spans;
        }
    }

    private bool IsIgnored(SyntaxNode node)
    {
        if (node.Span.End < _positionOfFirstReducingNullableDirective)
            return true;

        if (_ignoredSpans is not null)
        {
            if (_ignoredSpans.HasIntervalThatContains(node.SpanStart, node.Span.Length))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLanguageRestrictedToNonNullForm(TypeSyntax node)
    {
        // Simplify syntax checks by walking up qualified names to an equivalent parent node.
        node = WalkUpCurrentQualifiedName(node);

        if (node?.Parent is QualifiedNameSyntax qualifiedName
            && qualifiedName.Left == node)
        {
            // Cannot dot off a nullable reference type
            return true;
        }

        if (node.IsParentKind(SyntaxKind.UsingDirective))
        {
            // Using directives cannot directly reference a nullable reference type
            return true;
        }

        if (node.IsParentKind(SyntaxKind.SimpleBaseType))
        {
            // Cannot derive directly from a nullable reference type
            return true;
        }

        if (node?.Parent is BaseNamespaceDeclarationSyntax)
        {
            // Namespace names cannot be nullable reference types
            return true;
        }

        if (node.IsParentKind(SyntaxKind.NameEquals) && node.Parent.IsParentKind(SyntaxKind.UsingDirective))
        {
            // This is the alias or the target type of a using alias directive, neither of which can be nullable
            //
            //   using CustomException = System.Exception;
            //         ^^^^^^^^^^^^^^^   ^^^^^^^^^^^^^^^^
            return true;
        }

        return false;

        // If this is Y in X.Y, walk up to X.Y
        static TypeSyntax WalkUpCurrentQualifiedName(TypeSyntax node)
        {
            while (node.Parent is QualifiedNameSyntax qualifiedName
                && qualifiedName.Right == node)
            {
                node = qualifiedName;
            }

            return node;
        }
    }

    public void Dispose()
    {
    }

    public override void DefaultVisit(SyntaxNode node)
    {
        if (IsIgnored(node))
            return;

        if (node is TypeSyntax typeSyntax
            && !IsLanguageRestrictedToNonNullForm(typeSyntax))
        {
            if (typeSyntax.IsVar)
                return;

            if (typeSyntax is PredefinedTypeSyntax predefinedType
                && CSharpSyntaxFacts.Instance.TryGetPredefinedType(predefinedType.Keyword, out var type))
            {
                if (type is CodeAnalysis.LanguageService.PredefinedType.Object or CodeAnalysis.LanguageService.PredefinedType.String)
                {
                    SpansBuilder.Add(predefinedType.Span);
                }

                // All other predefined types are value types
                return;
            }

            var symbolInfo = _semanticModel.GetSymbolInfo(typeSyntax, _cancellationToken);
            if (symbolInfo.Symbol.IsKind(SymbolKind.Namespace))
            {
                // Namespaces cannot be nullable
                return;
            }
            else if (symbolInfo.Symbol is INamedTypeSymbol { IsValueType: true, IsGenericType: false })
            {
                return;
            }

            SpansBuilder.Add(node.Span);
            return;
        }

        base.DefaultVisit(node);
    }
}
