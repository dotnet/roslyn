// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal sealed class CSharpFormatter
{
    private const string MarkerId = "RazorMarker";

    public static async Task<IReadOnlyDictionary<int, int>> GetCSharpIndentationAsync(
        FormattingContext context,
        HashSet<int> projectedDocumentLocations,
        HostWorkspaceServices hostWorkspaceServices,
        CancellationToken cancellationToken)
    {
        // Sorting ensures we count the marker offsets correctly.
        // We also want to ensure there are no duplicates to avoid duplicate markers.
        var filteredLocations = projectedDocumentLocations.OrderAsArray();

        var indentations = await GetCSharpIndentationCoreAsync(context, filteredLocations, hostWorkspaceServices, cancellationToken).ConfigureAwait(false);
        return indentations;
    }

    private static async Task<Dictionary<int, int>> GetCSharpIndentationCoreAsync(
        FormattingContext context,
        ImmutableArray<int> projectedDocumentLocations,
        HostWorkspaceServices hostWorkspaceServices,
        CancellationToken cancellationToken)
    {
        // No point calling the C# formatting if we won't be interested in any of its work anyway
        if (projectedDocumentLocations.Length == 0)
        {
            return [];
        }

        var (indentationMap, syntaxTree) = await InitializeIndentationDataAsync(context, projectedDocumentLocations, cancellationToken).ConfigureAwait(false);

        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        root = AttachAnnotations(indentationMap, projectedDocumentLocations, root);

        // At this point, we have added all the necessary markers and attached annotations.
        // Let's invoke the C# formatter and hope for the best.
        var formattedRoot = RazorCSharpFormattingInteractionService.Format(hostWorkspaceServices, root, context.Options.ToIndentationOptions(), context.Options.CSharpSyntaxFormattingOptions, cancellationToken);
        var formattedText = formattedRoot.GetText();

        var desiredIndentationMap = new Dictionary<int, int>();

        // Assuming the C# formatter did the right thing, let's extract the indentation offset from
        // the line containing trivia and token that has our attached annotations.
        ExtractTriviaAnnotations(context, formattedRoot, formattedText, desiredIndentationMap);
        ExtractTokenAnnotations(context, formattedRoot, formattedText, indentationMap, desiredIndentationMap);

        return desiredIndentationMap;

        static void ExtractTriviaAnnotations(
            FormattingContext context,
            SyntaxNode formattedRoot,
            SourceText formattedText,
            Dictionary<int, int> desiredIndentationMap)
        {
            var formattedTriviaList = formattedRoot.GetAnnotatedTrivia(MarkerId);
            foreach (var trivia in formattedTriviaList)
            {
                // We only expect one annotation because we built the entire trivia with a single annotation, but
                // we need to be defensive here. Annotations are a little hard to work with though, so apologies for
                // the slightly odd method of validation.
                using var enumerator = trivia.GetAnnotations(MarkerId).GetEnumerator();
                enumerator.MoveNext();
                var annotation = enumerator.Current;
                // We shouldn't be able to enumerate any more, and we should be able to parse our data out of the annotation.
                if (enumerator.MoveNext() ||
                    !int.TryParse(annotation.Data, out var projectedIndex))
                {
                    // This shouldn't happen realistically unless someone messed with the annotations we added.
                    // Let's ignore this annotation.
                    continue;
                }

                var line = formattedText.Lines.GetLineFromPosition(trivia.SpanStart);
                var offset = GetIndentationOffsetFromLine(context, line);

                desiredIndentationMap[projectedIndex] = offset;
            }
        }

        static void ExtractTokenAnnotations(
            FormattingContext context,
            SyntaxNode formattedRoot,
            SourceText formattedText,
            Dictionary<int, IndentationMapData> indentationMap,
            Dictionary<int, int> desiredIndentationMap)
        {
            var formattedTokenList = formattedRoot.GetAnnotatedTokens(MarkerId);
            foreach (var token in formattedTokenList)
            {
                // There could be multiple annotations per token because a token can span multiple lines.
                // E.g, a multiline string literal.
                var annotations = token.GetAnnotations(MarkerId);
                foreach (var annotation in annotations)
                {
                    if (!int.TryParse(annotation.Data, out var projectedIndex))
                    {
                        // This shouldn't happen realistically unless someone messed with the annotations we added.
                        // Let's ignore this annotation.
                        continue;
                    }

                    var indentationMapData = indentationMap[projectedIndex];
                    var line = formattedText.Lines.GetLineFromPosition(token.SpanStart + indentationMapData.CharacterOffset);
                    var offset = GetIndentationOffsetFromLine(context, line);

                    // Every bit of C# in a Razor file is assumed to be indented by at least 2 levels (namespace and class)
                    // and the Razor formatter works based on that assumption. For some specific C# nodes however, the C# formatter
                    // will not indent them at all. When they happen to be indented more than 2 levels this causes a problem
                    // because we essentially assume that we should always move them left by at least 2 levels. This means that these
                    // nodes end up moving left with every format operation, until they hit the minimum of 2 indent levels.
                    // We can't fix this, so we just work around it by ignoring those lines completely, and leaving them where the
                    // user put them.

                    if (ShouldIgnoreLineCompletely(token, formattedText))
                    {
                        offset = -1;
                    }

                    desiredIndentationMap[projectedIndex] = offset;
                }
            }
        }
    }

    private static bool ShouldIgnoreLineCompletely(SyntaxToken token, SourceText text)
    {
        return ShouldIgnoreLineCompletelyBecauseOfNode(token.Parent, text)
            || ShouldIgnoreLineCompletelyBecauseOfAncestors(token, text);

        static bool ShouldIgnoreLineCompletelyBecauseOfNode(SyntaxNode? node, SourceText text)
        {
            return node switch
            {
                // We don't want to format lines that are part of multi-line string literals
                LiteralExpressionSyntax { RawKind: (int)CSharp.SyntaxKind.StringLiteralExpression } => SpansMultipleLines(node, text),
                // As above, but for multi-line interpolated strings
                InterpolatedStringExpressionSyntax => SpansMultipleLines(node, text),
                InterpolatedStringTextSyntax => SpansMultipleLines(node, text),
                _ => false
            };
        }

        static bool ShouldIgnoreLineCompletelyBecauseOfAncestors(SyntaxToken token, SourceText text)
        {
            var parent = token.Parent;
            if (parent is null)
            {
                return false;
            }

            // When directly in an implicit object creation expression, it seems the C# formatter
            // does format the braces of an array initializer, so we need to special case those
            // node types. Doing it outside the loop is good for perf, but also makes things easier.
            if (parent is InitializerExpressionSyntax initializer &&
                initializer.IsKind(CSharp.SyntaxKind.ArrayInitializerExpression) &&
                (token == initializer.OpenBraceToken || token == initializer.CloseBraceToken) &&
                initializer.Parent?.Parent?.Parent?.Parent is ImplicitObjectCreationExpressionSyntax)
            {
                return false;
            }

            return parent.AncestorsAndSelf().Any(node => node switch
            {
                CollectionExpressionSyntax collectionExpression => IgnoreCollectionExpression(collectionExpression, token),
                InitializerExpressionSyntax initializer => IgnoreInitializerExpression(initializer, token),
                _ => false
            });
        }

        static bool SpansMultipleLines(SyntaxNode node, SourceText text)
        {
            var range = text.GetRange(node.Span);
            return range.SpansMultipleLines();
        }

        static bool IgnoreCollectionExpression(CollectionExpressionSyntax collectionExpression, SyntaxToken token)
        {
            // We want to format the close brace otherwise it moves
            if (token == collectionExpression.CloseBracketToken)
            {
                return false;
            }

            // Ditto for the first element
            if (collectionExpression.Elements is [{ } first, ..] &&
                first.Contains(token.Parent))
            {
                return false;
            }

            // Otherwise, leave collection expressions alone
            return true;
        }

        static bool IgnoreInitializerExpression(InitializerExpressionSyntax initializer, SyntaxToken token)
        {
            if (initializer.IsKind(CSharp.SyntaxKind.ArrayInitializerExpression))
            {
                // For array initializers we don't want to ignore the open and close braces
                // as the formatter does move them relative to the variable declaration they
                // are part of, but doesn't otherwise touch them.
                // This isn't true if they are part of other collection or object initializers, but
                // fortunately we can ignore that because of the recursive nature of this method,
                // I just wanted to mention it so you understood how annoying this is :)
                // This also isn't true for the close brace token of an _implicit_ array creation
                // expression, because Roslyn was designed to hurt me.
                if (token == initializer.OpenBraceToken ||
                    (token == initializer.CloseBraceToken && initializer.Parent is not ImplicitArrayCreationExpressionSyntax))
                {
                    return false;
                }

                // Anything else in an array initializer we ignore
                return true;
            }

            // Any other type of initializer, as long as its not empty, we also ignore
            if (initializer.Expressions.Count > 0)
            {
                return true;
            }

            return false;
        }
    }

    private static async Task<(Dictionary<int, IndentationMapData>, SyntaxTree)> InitializeIndentationDataAsync(
        FormattingContext context,
        IEnumerable<int> projectedDocumentLocations,
        CancellationToken cancellationToken)
    {
        // The approach we're taking here is to add markers only when absolutely necessary.
        // We'll attach annotations to tokens directly when possible.

        var indentationMap = new Dictionary<int, IndentationMapData>();
        var marker = "/*__marker__*/";
        var markerString = $"{context.NewLineString}{marker}{context.NewLineString}";

        using var changes = new PooledArrayBuilder<TextChange>();

        var syntaxTree = await context.CurrentSnapshot.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        var previousMarkerOffset = 0;
        foreach (var projectedDocumentIndex in projectedDocumentLocations)
        {
            var token = root.FindToken(projectedDocumentIndex, findInsideTrivia: true);

            // We use a marker if the projected location is in trivia, because we can't add annotations to a specific piece of trivia
            var isInTrivia = projectedDocumentIndex < token.SpanStart || projectedDocumentIndex >= token.Span.End;
            if (isInTrivia)
            {
                // We want to add a marker here because the location points to a whitespace
                // which will not get preserved during formatting.

                // position points to the start of the /*__marker__*/ comment.
                var position = projectedDocumentIndex + context.NewLineString.Length;
                var change = new TextChange(new TextSpan(projectedDocumentIndex, 0), markerString);
                changes.Add(change);

                indentationMap.Add(projectedDocumentIndex, new IndentationMapData()
                {
                    OriginalProjectedDocumentIndex = projectedDocumentIndex,
                    AnnotationAttachIndex = position + previousMarkerOffset,
                    MarkerKind = MarkerKind.Trivia,
                });

                // We have added a marker. This means we need to account for the length of the marker in future calculations.
                previousMarkerOffset += markerString.Length;
            }
            else
            {
                // No marker needed. Let's attach the annotation directly at the given location.
                indentationMap.Add(projectedDocumentIndex, new IndentationMapData()
                {
                    OriginalProjectedDocumentIndex = projectedDocumentIndex,
                    AnnotationAttachIndex = projectedDocumentIndex + previousMarkerOffset,
                    MarkerKind = MarkerKind.Token,
                });
            }
        }

        var changedText = context.CSharpSourceText.WithChanges(changes.ToImmutable());
        return (indentationMap, syntaxTree.WithChangedText(changedText));
    }

    private static SyntaxNode AttachAnnotations(
        Dictionary<int, IndentationMapData> indentationMap,
        IEnumerable<int> projectedDocumentLocations,
        SyntaxNode root)
    {
        foreach (var projectedDocumentIndex in projectedDocumentLocations)
        {
            var indentationMapData = indentationMap[projectedDocumentIndex];
            var annotation = new SyntaxAnnotation(MarkerId, $"{projectedDocumentIndex}");

            if (indentationMapData.MarkerKind == MarkerKind.Trivia)
            {
                var trackingTrivia = root.FindTrivia(indentationMapData.AnnotationAttachIndex, findInsideTrivia: true);
                var annotatedTrivia = trackingTrivia.WithAdditionalAnnotations(annotation);
                root = root.ReplaceTrivia(trackingTrivia, annotatedTrivia);
            }
            else
            {
                var trackingToken = root.FindToken(indentationMapData.AnnotationAttachIndex, findInsideTrivia: true);
                var annotatedToken = trackingToken.WithAdditionalAnnotations(annotation);
                root = root.ReplaceToken(trackingToken, annotatedToken);

                // Since a token can span multiple lines, we need to keep track of the offset within the token span.
                // We will use this later when determining the exact line within a token in cases like a multiline string literal.
                indentationMapData.CharacterOffset = indentationMapData.AnnotationAttachIndex - trackingToken.SpanStart;
            }
        }

        return root;
    }

    private static int GetIndentationOffsetFromLine(FormattingContext context, TextLine line)
    {
        var offset = line.GetFirstNonWhitespaceOffset() ?? 0;
        if (!context.Options.InsertSpaces)
        {
            // Normalize to spaces because the rest of the formatting pipeline operates based on the assumption.
            offset *= (int)context.Options.TabSize;
        }

        return offset;
    }

    private class IndentationMapData
    {
        public int OriginalProjectedDocumentIndex { get; set; }

        public int AnnotationAttachIndex { get; set; }

        public int CharacterOffset { get; set; }

        public MarkerKind MarkerKind { get; set; }

        public override string ToString()
        {
            return $"Original: {OriginalProjectedDocumentIndex}, MarkerAdjusted: {AnnotationAttachIndex}, Kind: {MarkerKind}, TokenOffset: {CharacterOffset}";
        }
    }

    private enum MarkerKind
    {
        Trivia,
        Token
    }
}
