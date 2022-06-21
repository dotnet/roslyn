// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.RemoveUnnecessaryNullableDirective
{
    internal sealed class NullableImpactingSpanWalker : CSharpSyntaxWalker, IDisposable
    {
        private readonly SemanticModel _semanticModel;
        private readonly int _positionOfFirstReducingNullableDirective;
        private readonly SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector>? _ignoredSpans;
        private readonly CancellationToken _cancellationToken;

        private ImmutableArray<TextSpan>.Builder? _spans;

        public bool HasSpans => _spans?.Count > 0;

        public ImmutableArray<TextSpan> Spans => _spans?.ToImmutable() ?? ImmutableArray<TextSpan>.Empty;

        public ImmutableArray<TextSpan>.Builder SpansBuilder
        {
            get
            {
                if (_spans is null)
                    Interlocked.CompareExchange(ref _spans, ImmutableArray.CreateBuilder<TextSpan>(), null);

                return _spans;
            }
        }

        public NullableImpactingSpanWalker(
            SemanticModel semanticModel,
            int positionOfFirstReducingNullableDirective,
            SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector>? ignoredSpans,
            CancellationToken cancellationToken)
            : base(SyntaxWalkerDepth.StructuredTrivia)
        {
            _semanticModel = semanticModel;
            _positionOfFirstReducingNullableDirective = positionOfFirstReducingNullableDirective;
            _ignoredSpans = ignoredSpans;
            _cancellationToken = cancellationToken;
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

        public void Dispose()
        {
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            if (IsIgnored(node))
                return;

            if (node is TypeSyntax typeSyntax)
            {
                if (typeSyntax.IsVar)
                    return;

                if (typeSyntax.IsKind(SyntaxKind.PredefinedType, out PredefinedTypeSyntax? predefinedType)
                    && CSharpSyntaxFacts.Instance.TryGetPredefinedType(predefinedType.Keyword, out var type))
                {
                    if (type is CodeAnalysis.LanguageServices.PredefinedType.Object or CodeAnalysis.LanguageServices.PredefinedType.String)
                    {
                        SpansBuilder.Add(predefinedType.Span);
                    }

                    // All other predefined types are value types
                    return;
                }

                var symbolInfo = _semanticModel.GetSymbolInfo(typeSyntax, _cancellationToken);
                if (symbolInfo.Symbol is INamedTypeSymbol { IsValueType: true, IsGenericType: false })
                {
                    return;
                }

                SpansBuilder.Add(node.Span);
                return;
            }

            base.DefaultVisit(node);
        }
    }
}
