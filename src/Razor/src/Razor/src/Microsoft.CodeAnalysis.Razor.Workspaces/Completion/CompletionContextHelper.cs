// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Syntax;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal static class CompletionContextHelper
{
    /// <summary>
    /// Adjusts the syntax node owner to find the nearest start or end tag for completion purposes.
    /// </summary>
    /// <param name="owner">The original syntax node owner.</param>
    /// <returns>The adjusted owner node.</returns>
    public static RazorSyntaxNode? AdjustSyntaxNodeForCompletion(RazorSyntaxNode? owner)
        => owner switch
        {
            // This provider is trying to find the nearest Start or End tag. Most of the time, that's a level up, but if the index the user is typing at
            // is a token of a start or end tag directly, we already have the node we want.
            MarkupStartTagSyntax or MarkupEndTagSyntax or MarkupTagHelperStartTagSyntax or MarkupTagHelperEndTagSyntax or MarkupTagHelperAttributeSyntax => owner,
            // Invoking completion in an empty file will give us RazorDocumentSyntax which always has null parent
            RazorDocumentSyntax => owner,
            // Either the parent is a context we can handle, or it's not and we shouldn't show completions.
            _ => owner?.Parent
        };

    /// <summary>
    /// Determines if the absolute index is within an attribute name completion context.
    /// </summary>
    /// <param name="selectedAttributeName">The currently selected or partially typed attribute name.</param>
    /// <param name="selectedAttributeNameLocation">The location of the selected attribute name.</param>
    /// <param name="prefixLocation">The location of the attribute prefix (e.g., "@" for directive attributes).</param>
    /// <param name="absoluteIndex">The cursor position in the document.</param>
    /// <returns>True if the context is appropriate for attribute name completion, false otherwise.</returns>
    /// <remarks>
    /// To align with HTML completion behavior we only want to provide completion items if we're trying to resolve completion at the
    /// beginning of an HTML attribute name or at the end of possible partially written attribute. We do extra checks on prefix locations here in order to rule out malformed cases when the Razor
    /// compiler incorrectly parses multi-line attributes while in the middle of typing out an element. For instance:
    /// &lt;SurveyPrompt |
    /// @code { ... }
    /// Will be interpreted as having an `@code` attribute name due to multi-line attributes being a thing. Ultimately this is mostly a
    /// heuristic that we have to apply in order to workaround limitations of the Razor compiler.
    /// </remarks>
    public static bool IsAttributeNameCompletionContext(
        string? selectedAttributeName,
        Microsoft.CodeAnalysis.Text.TextSpan? selectedAttributeNameLocation,
        Microsoft.CodeAnalysis.Text.TextSpan? prefixLocation,
        int absoluteIndex)
    {
        return selectedAttributeName is null ||
            selectedAttributeNameLocation?.IntersectsWith(absoluteIndex) == true ||
            (prefixLocation?.IntersectsWith(absoluteIndex) ?? false);
    }
}
