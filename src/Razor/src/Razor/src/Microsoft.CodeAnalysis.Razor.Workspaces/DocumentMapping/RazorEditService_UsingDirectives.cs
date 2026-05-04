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
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

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
            var newText = GetUsingsText(usingDirectives: [], addedUsings, removedUsings: []);
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
                    builder.AppendLine(currentDirective.GetContent());
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
                builder.AppendLine(directive.GetContent());
            }

            foreach (var @namespace in remainingNew)
            {
                builder.AppendLine(GetUsingsText(@namespace));
            }

            var startPosition = existingUsings[0].Span.Start;
            var endPosition = existingUsings[^1].Span.End;

            endPosition = AdjustPositionToEndOfLine(endPosition, codeDocument.Source.Text);

            edits.Add(new RazorTextChange()
            {
                Span = RazorTextSpan.FromBounds(startPosition, endPosition),
                NewText = builder.ToString()
            });
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

        var newText = GetUsingsText(firstBlockOfUsings, addedUsings, removedUsings);
        edits.Add(new RazorTextChange()
        {
            Span = RazorTextSpan.FromBounds(startPosition, endPosition),
            NewText = newText
        });

        foreach (var node in remainingUsings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddRemoveEdit(ref edits, node, codeDocument.Source.Text);
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

    private static string GetUsingsText(ImmutableArray<RazorUsingDirectiveSyntax> usingDirectives, ImmutableArray<string> newUsings, ImmutableArray<string> removedUsings)
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
            AddIfNotRemoved(@namespace, directive);
        }

        return builder.ToString();

        void AddIfNotRemoved(string @namespace, RazorUsingDirectiveSyntax? directive)
        {
            if (directive is not null)
            {
                if (removedUsings.Contains(@namespace))
                {
                    return;
                }

                builder.Append(directive.GetContent());
            }
            else
            {
                builder.Append(GetUsingsText(@namespace));
            }

            builder.AppendLine();
        }
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
