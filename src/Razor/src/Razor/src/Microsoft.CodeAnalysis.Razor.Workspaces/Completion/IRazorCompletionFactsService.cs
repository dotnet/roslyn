// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal interface IRazorCompletionFactsService
{
    /// <summary>
    /// Returns completion items from all providers that are not
    /// <see cref="IHtmlDependentCompletionItemProvider"/>.
    /// </summary>
    /// <returns>
    /// The completion items and a flag indicating whether any HTML-dependent provider
    /// was skipped because it needs HTML completions.  The caller can use this to decide whether
    /// a follow-up <see cref="GetHtmlDependentCompletionItems"/> call is necessary.
    /// </returns>
    CompletionItemsResult GetCompletionItems(RazorCompletionContext razorCompletionContext);

    /// <summary>
    /// Returns completion items from <see cref="IHtmlDependentCompletionItemProvider"/>
    /// instances, with HTML labels available via the <paramref name="context"/>.
    /// </summary>
    ImmutableArray<RazorCompletionItem> GetHtmlDependentCompletionItems(RazorHtmlDependentCompletionContext context);
}
