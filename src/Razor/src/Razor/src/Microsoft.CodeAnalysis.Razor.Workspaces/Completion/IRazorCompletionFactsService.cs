// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal interface IRazorCompletionFactsService
{
    /// <summary>
    /// Returns completion items from all providers. Providers that need HTML labels
    /// (e.g., for tag helper element filtering) read them from
    /// <see cref="RazorCompletionContext.HtmlLabels"/>.
    /// </summary>
    ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext razorCompletionContext);
}
