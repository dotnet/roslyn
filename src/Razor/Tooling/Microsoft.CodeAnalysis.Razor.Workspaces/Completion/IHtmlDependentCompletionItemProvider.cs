// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.Completion;

/// <summary>
/// An <see cref="IRazorCompletionItemProvider"/> that can produce additional completion items
/// when HTML completion labels are available.  Providers implementing this interface are
/// excluded from the initial (concurrent) completion pass and instead run in a second pass
/// after HTML completions have been obtained.
/// </summary>
internal interface IHtmlDependentCompletionItemProvider : IRazorCompletionItemProvider
{
    /// <summary>
    /// Returns <see langword="true"/> if this provider needs HTML completion labels for the
    /// given <paramref name="context"/>.  When <see langword="false"/>, the phase-2 call can
    /// be skipped entirely.
    /// </summary>
    bool NeedsHtmlCompletions(RazorCompletionContext context);

    /// <summary>
    /// Returns completion items that depend on HTML completion labels for deduplication or
    /// discovery.
    /// </summary>
    ImmutableArray<RazorCompletionItem> GetHtmlDependentCompletionItems(RazorHtmlDependentCompletionContext context);
}
