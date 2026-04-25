// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal class WrapAttributesCodeActionProvider : IRazorCodeActionProvider
{
    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        if (context.HasSelection)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        if (!context.CodeDocument.TryGetSyntaxRoot(out var root))
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var owner = root.FindNode(TextSpan.FromBounds(context.StartAbsoluteIndex, context.EndAbsoluteIndex));
        var attributes = FindAttributes(owner);
        if (attributes.Count == 0)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var first = true;
        var firstAttributeLine = 0;
        var indentSize = 0;
        var sourceText = context.SourceText;

        using var newLinePositions = new PooledArrayBuilder<int>(attributes.Count);
        foreach (var attribute in attributes)
        {
            var linePositionSpan = attribute.GetLinePositionSpan(context.CodeDocument.Source);

            if (first)
            {
                firstAttributeLine = linePositionSpan.Start.Line;
                sourceText.TryGetFirstNonWhitespaceOffset(attribute.Span, out var indentSizeOffset);
                indentSize = linePositionSpan.Start.Character + indentSizeOffset;
                first = false;
            }
            else
            {
                if (linePositionSpan.Start.Line != firstAttributeLine)
                {
                    return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
                }

                if (!sourceText.TryGetFirstNonWhitespaceOffset(attribute.Span, out var startOffset))
                {
                    continue;
                }

                newLinePositions.Add(attribute.SpanStart + startOffset);
            }
        }

        if (newLinePositions.Count == 0)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var data = new WrapAttributesCodeActionParams
        {
            IndentSize = indentSize,
            NewLinePositions = newLinePositions.ToArray()
        };

        var resolutionParams = new RazorCodeActionResolutionParams()
        {
            TextDocument = context.Request.TextDocument,
            Action = LanguageServerConstants.CodeActions.WrapAttributes,
            Language = RazorLanguageKind.Razor,
            DelegatedDocumentUri = context.DelegatedDocumentUri,
            Data = data
        };

        var action = RazorCodeActionFactory.CreateWrapAttributes(resolutionParams);

        return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([action]);
    }

    private static AspNetCore.Razor.Language.Syntax.SyntaxList<RazorSyntaxNode> FindAttributes(AspNetCore.Razor.Language.Syntax.SyntaxNode? owner)
    {
        // Sometimes FindNode will find the start tag, sometimes the element. We always start from the start tag to make searching
        // easier, and since we are concerned with attributes, things without start tags wouldn't be applicalbe anyway
        if (owner is MarkupElementSyntax element)
        {
            owner = element.StartTag;
        }
        else if (owner is MarkupTagHelperElementSyntax tagHelperElement)
        {
            owner = tagHelperElement.StartTag;
        }

        if (owner is null)
        {
            return [];
        }

        foreach (var node in owner.AncestorsAndSelf())
        {
            if (node is MarkupStartTagSyntax startTag)
            {
                return startTag.Attributes;
            }
            else if (node is MarkupTagHelperStartTagSyntax tagHelperElement)
            {
                return tagHelperElement.Attributes;
            }
            else if (node is MarkupElementSyntax or MarkupTagHelperElementSyntax)
            {
                // If we get as high as the element, we're done looking
                break;
            }
        }

        return [];
    }
}
