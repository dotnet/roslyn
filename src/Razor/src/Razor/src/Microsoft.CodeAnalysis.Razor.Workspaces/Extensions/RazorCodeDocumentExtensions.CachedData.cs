// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Threading;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class RazorCodeDocumentExtensions
{
    private static readonly ConditionalWeakTable<RazorCodeDocument, CachedData> s_codeDocumentCache = new();

    private static CachedData GetCachedData(RazorCodeDocument codeDocument)
        => s_codeDocumentCache.GetValue(codeDocument, static doc => new CachedData(doc));

    private sealed class CachedData(RazorCodeDocument codeDocument)
    {
        private readonly RazorCodeDocument _codeDocument = codeDocument;

        private readonly SemaphoreSlim _stateLock = new(initialCount: 1);
        private ImmutableArray<ClassifiedSpan>? _classifiedSpans;
        private ImmutableArray<SourceSpan>? _tagHelperSpans;
        private RazorHtmlDocument? _htmlDocument;

        public ImmutableArray<ClassifiedSpan> GetOrComputeClassifiedSpans(CancellationToken cancellationToken)
        {
            if (_classifiedSpans is { } classifiedSpans)
            {
                return classifiedSpans;
            }

            using (_stateLock.DisposableWait(cancellationToken))
            {
                return _classifiedSpans ??= ClassifiedSpanVisitor.VisitRoot(_codeDocument.GetRequiredTagHelperRewrittenSyntaxTree());
            }
        }

        public ImmutableArray<SourceSpan> GetOrComputeTagHelperSpans(CancellationToken cancellationToken)
        {
            if (_tagHelperSpans is { } tagHelperSpans)
            {
                return tagHelperSpans;
            }

            using (_stateLock.DisposableWait(cancellationToken))
            {
                return _tagHelperSpans ??= ComputeTagHelperSpans(_codeDocument.GetRequiredTagHelperRewrittenSyntaxTree());
            }

            static ImmutableArray<SourceSpan> ComputeTagHelperSpans(RazorSyntaxTree syntaxTree)
            {
                using var builder = new PooledArrayBuilder<SourceSpan>();

                foreach (var node in syntaxTree.Root.DescendantNodes())
                {
                    if (node is not MarkupTagHelperElementSyntax tagHelperElement ||
                        tagHelperElement.TagHelperInfo is null)
                    {
                        continue;
                    }

                    builder.Add(tagHelperElement.GetSourceSpan(syntaxTree.Source));
                }

                return builder.ToImmutableAndClear();
            }
        }

        public RazorHtmlDocument GetOrComputeHtmlDocument(CancellationToken cancellationToken)
        {
            if (_htmlDocument is not null)
            {
                return _htmlDocument;
            }

            using (_stateLock.DisposableWait(cancellationToken))
            {
                return _htmlDocument ??= RazorHtmlWriter.GetHtmlDocument(_codeDocument);
            }
        }
    }
}
