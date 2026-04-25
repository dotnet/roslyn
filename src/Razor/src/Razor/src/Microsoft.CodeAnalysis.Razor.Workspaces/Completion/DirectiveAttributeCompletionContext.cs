// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;

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

    public bool AlreadySatisfiesParameter(BoundAttributeParameterDescriptor parameter, BoundAttributeDescriptor attribute)
        => ExistingAttributes.Any(
            (parameter, attribute),
            static (name, arg) =>
                TagHelperMatchingConventions.SatisfiesBoundAttributeWithParameter(arg.parameter, name, arg.attribute));

    public bool CanSatisfyAttribute(BoundAttributeDescriptor attribute)
        => TagHelperMatchingConventions.CanSatisfyBoundAttribute(SelectedAttributeName, attribute);
}
