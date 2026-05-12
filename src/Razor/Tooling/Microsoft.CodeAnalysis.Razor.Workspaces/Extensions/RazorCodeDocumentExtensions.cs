// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

internal static partial class RazorCodeDocumentExtensions
{
    public static bool TryGetSyntaxRoot(this RazorCodeDocument codeDocument, [NotNullWhen(true)] out Syntax.SyntaxNode? result)
    {
        if (codeDocument.TryGetTagHelperRewrittenSyntaxTree(out var syntaxTree))
        {
            result = syntaxTree.Root;
            return true;
        }

        result = null;
        return false;
    }

    public static Syntax.SyntaxNode GetRequiredSyntaxRoot(this RazorCodeDocument codeDocument)
        => codeDocument.GetRequiredTagHelperRewrittenSyntaxTree().Root;

    public static SourceText GetCSharpSourceText(this RazorCodeDocument document)
        => document.GetRequiredCSharpDocument().Text;

    public static SourceText GetHtmlSourceText(this RazorCodeDocument document, CancellationToken cancellationToken)
        => GetCachedData(document).GetOrComputeHtmlDocument(cancellationToken).Text;

    public static bool TryGetMinimalCSharpRange(this RazorCodeDocument codeDocument, LinePositionSpan razorRange, out LinePositionSpan csharpRange)
    {
        SourceSpan? minGeneratedSpan = null;
        SourceSpan? maxGeneratedSpan = null;

        var sourceText = codeDocument.Source.Text;
        var textSpan = sourceText.GetTextSpan(razorRange);
        var csharpDoc = codeDocument.GetRequiredCSharpDocument();

        // We want to find the min and max C# source mapping that corresponds with our Razor range.
        foreach (var mapping in csharpDoc.SourceMappingsSortedByOriginal)
        {
            var mappedTextSpan = mapping.OriginalSpan.AsTextSpan();

            if (textSpan.OverlapsWith(mappedTextSpan))
            {
                if (minGeneratedSpan is null || mapping.GeneratedSpan.AbsoluteIndex < minGeneratedSpan.Value.AbsoluteIndex)
                {
                    minGeneratedSpan = mapping.GeneratedSpan;
                }

                var mappingEndIndex = mapping.GeneratedSpan.AbsoluteIndex + mapping.GeneratedSpan.Length;
                if (maxGeneratedSpan is null || mappingEndIndex > maxGeneratedSpan.Value.AbsoluteIndex + maxGeneratedSpan.Value.Length)
                {
                    maxGeneratedSpan = mapping.GeneratedSpan;
                }
            }
            else if (mappedTextSpan.Start > textSpan.End)
            {
                // This span (and all following) are after the area we're interested in
                break;
            }
        }

        // Create a new projected range based on our calculated min/max source spans.
        if (minGeneratedSpan is not null && maxGeneratedSpan is not null)
        {
            var csharpSourceText = codeDocument.GetCSharpSourceText();
            var startRange = csharpSourceText.GetLinePositionSpan(minGeneratedSpan.Value);
            var endRange = csharpSourceText.GetLinePositionSpan(maxGeneratedSpan.Value);

            csharpRange = new LinePositionSpan(startRange.Start, endRange.End);
            Debug.Assert(csharpRange.Start.CompareTo(csharpRange.End) <= 0, "Range.Start should not be larger than Range.End");

            return true;
        }

        csharpRange = default;
        return false;
    }

    public static bool ComponentNamespaceMatches(this RazorCodeDocument razorCodeDocument, string? fullyQualifiedNamespace)
    {
        var namespaceNode = (NamespaceDeclarationIntermediateNode)razorCodeDocument
            .GetRequiredDocumentNode()
            .FindDescendantNodes<IntermediateNode>()
            .First(static n => n is NamespaceDeclarationIntermediateNode);

        return namespaceNode.Name == fullyQualifiedNamespace;
    }

    public static RazorLanguageKind GetLanguageKind(this RazorCodeDocument codeDocument, int hostDocumentIndex, bool rightAssociative)
    {
        var classifiedSpans = GetClassifiedSpans(codeDocument);
        var tagHelperSpans = GetTagHelperSpans(codeDocument);
        var documentLength = codeDocument.Source.Text.Length;

        return GetLanguageKindCore(classifiedSpans, tagHelperSpans, hostDocumentIndex, documentLength, rightAssociative);
    }

    private static ImmutableArray<ClassifiedSpan> GetClassifiedSpans(RazorCodeDocument document)
        => GetCachedData(document).GetOrComputeClassifiedSpans(CancellationToken.None);

    private static ImmutableArray<SourceSpan> GetTagHelperSpans(RazorCodeDocument document)
        => GetCachedData(document).GetOrComputeTagHelperSpans(CancellationToken.None);

    private static RazorLanguageKind GetLanguageKindCore(
        ImmutableArray<ClassifiedSpan> classifiedSpans,
        ImmutableArray<SourceSpan> tagHelperSpans,
        int hostDocumentIndex,
        int hostDocumentLength,
        bool rightAssociative)
    {
        var length = classifiedSpans.Length;
        for (var i = 0; i < length; i++)
        {
            var classifiedSpan = classifiedSpans[i];
            var span = classifiedSpan.Span;

            if (span.AbsoluteIndex <= hostDocumentIndex)
            {
                var end = span.AbsoluteIndex + span.Length;
                if (end >= hostDocumentIndex)
                {
                    if (end == hostDocumentIndex)
                    {
                        // We're at an edge.

                        if (classifiedSpan.Kind is SpanKind.MetaCode or SpanKind.Transition)
                        {
                            // If we're on an edge of a transition of some kind (MetaCode representing an open or closing piece of syntax such as <|,
                            // and Transition representing an explicit transition to/from razor syntax, such as @|), prefer to classify to the span
                            // to the right to better represent where the user clicks
                            continue;
                        }

                        // If we're right associative, then we don't want to use the classification that we're at the end
                        // of, if we're also at the start of the next one
                        if (rightAssociative)
                        {
                            if (i < classifiedSpans.Length - 1 && classifiedSpans[i + 1].Span.AbsoluteIndex == hostDocumentIndex)
                            {
                                // If we're at the start of the next span, then use that span
                                return GetLanguageFromClassifiedSpan(classifiedSpans[i + 1]);
                            }

                            // Otherwise, we did not find a match using right associativity, so check for tag helpers
                            break;
                        }
                    }

                    return GetLanguageFromClassifiedSpan(classifiedSpan);
                }
            }
        }

        foreach (var span in tagHelperSpans)
        {
            if (span.AbsoluteIndex <= hostDocumentIndex)
            {
                var end = span.AbsoluteIndex + span.Length;
                if (end >= hostDocumentIndex)
                {
                    if (end == hostDocumentIndex)
                    {
                        // We're at an edge. TagHelper spans never own their edge and aren't represented by marker spans
                        continue;
                    }

                    // Found intersection
                    return RazorLanguageKind.Html;
                }
            }
        }

        // Use the language of the last classified span if we're at the end
        // of the document.
        if (classifiedSpans.Length != 0 && hostDocumentIndex == hostDocumentLength)
        {
            var lastClassifiedSpan = classifiedSpans.Last();
            return GetLanguageFromClassifiedSpan(lastClassifiedSpan);
        }

        // Default to Razor
        return RazorLanguageKind.Razor;

        static RazorLanguageKind GetLanguageFromClassifiedSpan(ClassifiedSpan classifiedSpan)
        {
            // Overlaps with request
            return classifiedSpan.Kind switch
            {
                SpanKind.Markup => RazorLanguageKind.Html,
                SpanKind.Code => RazorLanguageKind.CSharp,

                // Content type was non-C# or Html or we couldn't find a classified span overlapping the request position.
                // All other classified span kinds default back to Razor
                _ => RazorLanguageKind.Razor,
            };
        }
    }
}
