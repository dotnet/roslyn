// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

internal partial class RazorEditService
{
    /// <summary>
    /// Given a set of new and removed usings, adds text changes to this builder using the following logic:
    ///
    /// <list type="number">
    /// <item>
    /// If there are no existing usings the new usings are added at the top of the document following any page, component, or namespace directives.
    /// </item>
    ///
    /// <item>
    /// If there are existing usings but they are in a continuous block, replace that block with the new ordered set of usings.
    /// </item>
    ///
    /// <item>
    /// If for some reason a user has usings not in a single block (allows for whitespace), then replace the first block of using directives
    /// with the set of usings within that block that have not been removed AND the new usings. The remaining directives outside the block are removed
    /// as needed.
    /// </item>
    /// </list>
    /// </summary>
    private static void AddUsingsChanges(
        ref PooledArrayBuilder<RazorTextChange> edits,
        RazorCodeDocument codeDocument,
        ImmutableArray<string> addedUsings,
        ImmutableArray<string> removedUsings,
        CancellationToken cancellationToken)
    {
        if (addedUsings.Length == 0 && removedUsings.Length == 0)
        {
            return;
        }

        // If only usings were added then just need to find where to insert them.
        if (removedUsings.Length == 0)
        {
            AddNewUsingsChanges(ref edits, codeDocument, addedUsings, cancellationToken);
            return;
        }

        // If only usings are being removed complex logic can be avoided
        if (addedUsings.Length == 0)
        {
            AddRemoveUsingsChanges(ref edits, codeDocument, removedUsings, cancellationToken);
            return;
        }

        AddComplexUsingsChanges(ref edits, codeDocument, addedUsings, removedUsings, cancellationToken);
    }

    private static void AddNewUsingsChanges(
        ref PooledArrayBuilder<RazorTextChange> edits,
        RazorCodeDocument codeDocument,
        ImmutableArray<string> addedUsings,
        CancellationToken cancellationToken)
    {
        var (firstBlockOfUsings, remainingUsings) = GetGroupedUsings(codeDocument, cancellationToken);

        // If no usings are present then simply add all the usings as a block
        if (firstBlockOfUsings.Length == 0)
        {
            Debug.Assert(remainingUsings.IsEmpty, "Should not have no first block but still have remaining usings");
            var span = FindFirstTopLevelSpotForUsing(codeDocument);
            var newText = GetUsingsText(usingDirectives: [], addedUsings, removedUsings: [], ref edits);
            edits.Add(new RazorTextChange()
            {
                Span = span,
                NewText = newText
            });

            return;
        }

        AddNewUsingsToBlock(ref edits, firstBlockOfUsings, addedUsings);

        static RazorTextSpan FindFirstTopLevelSpotForUsing(RazorCodeDocument codeDocument)
        {
            var root = codeDocument.GetRequiredSyntaxRoot();
            var nodeToInsertAfter = root
                .DescendantNodes()
                .LastOrDefault(t => t is RazorDirectiveSyntax directiveNode
                && (directiveNode.IsDirective(ComponentPageDirective.Directive)
                    || directiveNode.IsDirective(NamespaceDirective.Directive)
                    || directiveNode.IsDirective(PageDirective.Directive)));

            if (nodeToInsertAfter is null)
            {
                return new RazorTextSpan();
            }

            var start = nodeToInsertAfter.Span.End;
            return new RazorTextSpan
            {
                Start = start,
                Length = 0
            };
        }

        void AddNewUsingsToBlock(ref PooledArrayBuilder<RazorTextChange> edits, ImmutableArray<RazorUsingDirectiveSyntax> existingUsings, ImmutableArray<string> addedUsings)
        {
            Debug.Assert(existingUsings.Length > 0);
            using var _ = StringBuilderPool.GetPooledObject(out var builder);

            var orderedExistingUsings = existingUsings.OrderAsArray(UsingsNodeComparer.Instance);
            var orderedAddedUsings = addedUsings.OrderAsArray(UsingsStringComparer.Instance);

            var remainingExisting = orderedExistingUsings.AsSpan();
            var remainingNew = orderedAddedUsings.AsSpan();

            while (remainingExisting is not [] && remainingNew is not [])
            {
                var currentDirective = remainingExisting[0];
                var nextNew = remainingNew[0];

                RazorSyntaxFacts.TryGetNamespaceFromDirective(currentDirective, out var currentNamespace);
                var comparand = UsingsStringComparer.Instance.Compare(currentNamespace, nextNew);
                if (comparand < 0)
                {
                    // Current namespace goes before new namespace
                    builder.AppendLine(GetUsingDirectiveContent(currentDirective, ref edits));
                    remainingExisting = remainingExisting[1..];
                }
                else if (comparand > 0)
                {
                    // New namespace goes before current namespace
                    builder.AppendLine(GetUsingsText(nextNew));
                    remainingNew = remainingNew[1..];
                }

                Debug.Assert(comparand != 0, "New namespace should never be an existing namespace");
            }

            Debug.Assert(remainingNew.IsEmpty || remainingExisting.IsEmpty, "Should have consumed all new or existing usings");

            foreach (var directive in remainingExisting)
            {
                builder.AppendLine(GetUsingDirectiveContent(directive, ref edits));
            }

            foreach (var @namespace in remainingNew)
            {
                builder.AppendLine(GetUsingsText(@namespace));
            }

            var startPosition = existingUsings[0].Span.Start;
            var endPosition = existingUsings[^1].Span.End;

            endPosition = AdjustPositionToEndOfLine(endPosition, codeDocument.Source.Text);

            AddUsingBlockEdit(ref edits, TextSpan.FromBounds(startPosition, endPosition), builder.ToString());
        }
    }

    private static void AddRemoveUsingsChanges(ref PooledArrayBuilder<RazorTextChange> edits, RazorCodeDocument codeDocument, ImmutableArray<string> removedUsings, CancellationToken cancellationToken)
    {
        var (firstBlockOfUsings, remainingUsings) = GetGroupedUsings(codeDocument, cancellationToken);
        var allUsingNodes = firstBlockOfUsings.Concat(remainingUsings);
        foreach (var node in allUsingNodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RazorSyntaxFacts.TryGetNamespaceFromDirective(node, out var @namespace);
            @namespace.AssumeNotNull();
            if (removedUsings.Contains(@namespace))
            {
                AddRemoveEdit(ref edits, node, codeDocument.Source.Text);
            }
        }
    }

    private static void AddComplexUsingsChanges(
        ref PooledArrayBuilder<RazorTextChange> edits,
        RazorCodeDocument codeDocument,
        ImmutableArray<string> addedUsings,
        ImmutableArray<string> removedUsings,
        CancellationToken cancellationToken)
    {
        Debug.Assert(addedUsings.Length > 0, "There should be at least one added using for complex changes");
        Debug.Assert(removedUsings.Length > 0, "There should be at least one removed using for complex changes");

        var (firstBlockOfUsings, remainingUsings) = GetGroupedUsings(codeDocument, cancellationToken);

        Debug.Assert(firstBlockOfUsings.Length > 0, "There should be at least one using directive in the first block");

        var startPosition = firstBlockOfUsings[0].Span.Start;
        var endPosition = firstBlockOfUsings[^1].Span.End;

        endPosition = AdjustPositionToEndOfLine(endPosition, codeDocument.Source.Text);

        var newText = GetUsingsText(firstBlockOfUsings, addedUsings, removedUsings, ref edits);
        AddUsingBlockEdit(ref edits, TextSpan.FromBounds(startPosition, endPosition), newText);

        // New imports are already in the first block. Leave other directives in place, including aliases
        // with identifier edits mapped directly rather than represented in removedUsings.
        foreach (var node in remainingUsings)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RazorSyntaxFacts.TryGetNamespaceFromDirective(node, out var @namespace);
            @namespace.AssumeNotNull();
            if (removedUsings.Contains(@namespace))
            {
                AddRemoveEdit(ref edits, node, codeDocument.Source.Text);
            }
        }
    }

    private static void AddRemoveEdit(ref PooledArrayBuilder<RazorTextChange> edits, RazorUsingDirectiveSyntax node, SourceText text)
    {
        var start = node.Span.Start;
        var end = AdjustPositionToEndOfLine(node.Span.End, text);
        edits.Add(new RazorTextChange()
        {
            Span = RazorTextSpan.FromBounds(start, end),
            NewText = ""
        });
    }

    private static int AdjustPositionToEndOfLine(int endPosition, SourceText text)
    {
        if (endPosition >= text.Length)
        {
            return endPosition;
        }

        if (text[endPosition] == '\r')
        {
            endPosition++;
        }

        if (endPosition >= text.Length)
        {
            return endPosition;
        }

        if (text[endPosition] == '\n')
        {
            return endPosition + 1;
        }

        return endPosition;
    }

    private static string GetUsingsText(string @namespace)
        => $"@using {@namespace}";

    private static string GetUsingsText(
        ImmutableArray<RazorUsingDirectiveSyntax> usingDirectives,
        ImmutableArray<string> newUsings,
        ImmutableArray<string> removedUsings,
        ref PooledArrayBuilder<RazorTextChange> mappedEdits)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        var usingsMap = new Dictionary<string, RazorUsingDirectiveSyntax?>(newUsings.Length + usingDirectives.Length);
        foreach (var @using in newUsings)
        {
            usingsMap.Add(@using, null);
        }

        foreach (var directive in usingDirectives)
        {
            if (RazorSyntaxFacts.TryGetNamespaceFromDirective(directive, out var @namespace))
            {
                usingsMap[@namespace] = directive;
            }
        }

        if (usingsMap.Count == 0)
        {
            return "";
        }

        var sortedUsingsAndDirectives = usingsMap
            .OrderByAsArray(static kvp => kvp.Key, UsingsStringComparer.Instance);

        foreach (var (@namespace, directive) in sortedUsingsAndDirectives)
        {
            AddIfNotRemoved(@namespace, directive, ref mappedEdits);
        }

        return builder.ToString();

        void AddIfNotRemoved(string @namespace, RazorUsingDirectiveSyntax? directive, ref PooledArrayBuilder<RazorTextChange> mappedEdits)
        {
            if (directive is not null)
            {
                if (removedUsings.Contains(@namespace))
                {
                    return;
                }

                builder.Append(GetUsingDirectiveContent(directive, ref mappedEdits));
            }
            else
            {
                builder.Append(GetUsingsText(@namespace));
            }

            builder.AppendLine();
        }
    }

    private static string GetUsingDirectiveContent(RazorUsingDirectiveSyntax directive, ref PooledArrayBuilder<RazorTextChange> mappedEdits)
    {
        // When rebuilding the using block, include any edits already mapped to this directive so
        // those changes aren't lost. To apply them to the directive's text, convert their positions
        // from offsets in the whole Razor document to offsets from the start of the directive.
        // An insertion at the end belongs after the directive, such as a new @code block at EOF.
        using var changes = new PooledArrayBuilder<TextChange>();
        foreach (var edit in mappedEdits)
        {
            if (directive.Span.Contains(edit.Span.ToTextSpan()) && edit.Span.Start < directive.Span.End)
            {
                changes.Add(new TextChange(new TextSpan(edit.Span.Start - directive.SpanStart, edit.Span.Length), edit.NewText ?? ""));
            }
        }

        // Keep the original text and formatting, changing only the mapped identifiers. Leave the
        // pooled edits untouched here; AddUsingBlockEdit removes them after the whole block is rendered.
        var content = directive.GetContent();
        return changes.Count == 0
            ? content
            : SourceText.From(content).WithChanges(changes.ToImmutableAndClear()).ToString();
    }

    private static void AddUsingBlockEdit(ref PooledArrayBuilder<RazorTextChange> edits, TextSpan span, string newText)
    {
        // If there are mapped edits within this span, the replacement already includes them. Remove their
        // originals so they cannot overlap the block edit or be discarded by normalization.
        // Keep insertions at the end boundary: they weren't incorporated into the directive text.
        for (var i = edits.Count - 1; i >= 0; i--)
        {
            if (span.Contains(edits[i].Span.ToTextSpan()) && edits[i].Span.Start < span.End)
            {
                edits.RemoveAt(i);
            }
        }

        edits.Add(new RazorTextChange
        {
            Span = new RazorTextSpan { Start = span.Start, Length = span.Length },
            NewText = newText
        });
    }

    private static (ImmutableArray<RazorUsingDirectiveSyntax> firstBlockOfUsings, ImmutableArray<RazorUsingDirectiveSyntax> remainingUsings) GetGroupedUsings(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        // It's not guaranteed that usings are continuous so this code has to account for that.
        // The logic is as follows:
        // All usings that are in a continuous block are bulk replaced with the set containing them and the new using directives.
        // All usings outside of the continuous block are checked to see if they need to be removed

        var root = codeDocument.GetRequiredSyntaxRoot();
        using var firstBlockOfUsingsBuilder = new PooledArrayBuilder<RazorUsingDirectiveSyntax>();
        using var remainingUsingsBuilder = new PooledArrayBuilder<RazorUsingDirectiveSyntax>();
        var allUsingsInSameBlock = true;

        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node is RazorUsingDirectiveSyntax razorDirective)
            {
                if (!allUsingsInSameBlock)
                {
                    remainingUsingsBuilder.Add(razorDirective);
                    continue;
                }

                if (firstBlockOfUsingsBuilder.Count == 0)
                {
                    firstBlockOfUsingsBuilder.Add(razorDirective);
                }
                else if (firstBlockOfUsingsBuilder[^1].IsNextTo(razorDirective, codeDocument.Source.Text))
                {
                    firstBlockOfUsingsBuilder.Add(razorDirective);
                }
                else
                {
                    remainingUsingsBuilder.Add(razorDirective);
                    allUsingsInSameBlock = false;
                }
            }
        }

        return (firstBlockOfUsingsBuilder.ToImmutableAndClear(), remainingUsingsBuilder.ToImmutableAndClear());
    }
}
