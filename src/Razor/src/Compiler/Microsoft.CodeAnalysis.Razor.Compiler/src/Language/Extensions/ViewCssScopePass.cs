// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class ViewCssScopePass : IntermediateNodePassBase, IRazorOptimizationPass
{
    // Runs after taghelpers are bound
    public override int Order => 110;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        var cssScope = codeDocument.CodeGenerationOptions.CssScope;
        if (string.IsNullOrEmpty(cssScope))
        {
            return;
        }

        if (!string.Equals(documentNode.DocumentKind, "mvc.1.0.view", StringComparison.Ordinal) &&
            !string.Equals(documentNode.DocumentKind, "mvc.1.0.razor-page", StringComparison.Ordinal))
        {
            return;
        }

        var scopeWithSeparator = " " + cssScope;
        IntermediateToken? previousToken = null;
        foreach (var node in documentNode.FindDescendantNodes<HtmlContentIntermediateNode>())
        {
            ProcessElement(node, scopeWithSeparator, ref previousToken);
        }
    }

    private void ProcessElement(HtmlContentIntermediateNode node, string cssScope, ref IntermediateToken? previousToken)
    {
        // Add a minimized attribute whose name is simply the CSS scope
        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            if (child is HtmlIntermediateToken token)
            {
                if (IsValidElement(token, previousToken))
                {
                    node.Children.Insert(i + 1, IntermediateNodeFactory.HtmlToken(cssScope));
                    i++;
                }

                previousToken = token;
            }
        }

        static bool IsValidElement(IntermediateToken token, IntermediateToken? previousToken)
        {
            var content = token.Content.AsSpan();

            // `<!tag` is lowered into separate nodes `<` and `tag`, we process the latter.
            if (previousToken?.Content == "<" && content is [not '<', ..])
            {
                // There is no leading `<` to trim.
            }
            // Otherwise process the token if it starts with `<` but ignore if it is *only* `<`.
            else if (content is ['<', _, ..])
            {
                // Trim the leading `<`.
                content = content[1..];
            }
            else
            {
                return false;
            }

            /// <remarks>
            /// We want to avoid adding the CSS scope to elements that do not appear
            /// within the body element of the document. When this pass executes over the
            /// nodes, we don't have the ability to store whether we are a descendant of a
            /// `head` or `body` element so it is not possible to discern whether the tag
            /// is valid this way. Instead, we go for a straight-forward check on the tag
            /// name that we are currently inspecting.
            /// </remarks>
            return !content.StartsWith("/".AsSpan(), StringComparison.Ordinal)
                && !content.StartsWith("!".AsSpan(), StringComparison.Ordinal)
                && !content.Equals("head".AsSpan(), StringComparison.OrdinalIgnoreCase)
                && !content.Equals("meta".AsSpan(), StringComparison.OrdinalIgnoreCase)
                && !content.Equals("title".AsSpan(), StringComparison.OrdinalIgnoreCase)
                && !content.Equals("link".AsSpan(), StringComparison.OrdinalIgnoreCase)
                && !content.Equals("base".AsSpan(), StringComparison.OrdinalIgnoreCase)
                && !content.Equals("script".AsSpan(), StringComparison.OrdinalIgnoreCase)
                && !content.Equals("style".AsSpan(), StringComparison.OrdinalIgnoreCase)
                && !content.Equals("html".AsSpan(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
