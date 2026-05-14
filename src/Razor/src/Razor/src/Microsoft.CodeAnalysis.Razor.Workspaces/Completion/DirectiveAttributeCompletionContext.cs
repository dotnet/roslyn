// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal sealed record DirectiveAttributeCompletionContext
{
    public required string SelectedAttributeName { get; init; }
    public string? SelectedParameterName { get; init; }
    public ImmutableArray<string> ExistingAttributes { get; init => field = value.NullToEmpty(); } = [];
    public bool UseSnippets { get; init; } = true;
    public bool InAttributeName { get; init; } = true;
    public bool InParameterName { get; init; }
    public RazorCompletionOptions Options { get; init; }

    /// <summary>
    /// The range in the source document (excluding the leading '@') that should be replaced
    /// when a directive attribute completion is committed. Used for items that include a colon
    /// and parameter name (e.g., "bind-Value:after") to prevent duplication when the editor's
    /// word-boundary heuristic doesn't extend past the ':'.
    /// </summary>
    public LinePositionSpan? ReplacementRange { get; init; }

    public bool AlreadySatisfiesParameter(BoundAttributeParameterDescriptor parameter, BoundAttributeDescriptor attribute)
        => ExistingAttributes.Any(
            (parameter, attribute),
            static (name, arg) =>
                TagHelperMatchingConventions.SatisfiesBoundAttributeWithParameter(arg.parameter, name, arg.attribute));

    public bool CanSatisfyAttribute(BoundAttributeDescriptor attribute)
        => TagHelperMatchingConventions.CanSatisfyBoundAttribute(SelectedAttributeName, attribute);
}
