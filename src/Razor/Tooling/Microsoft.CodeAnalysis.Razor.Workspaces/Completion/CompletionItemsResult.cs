// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal readonly record struct CompletionItemsResult(
    ImmutableArray<RazorCompletionItem> Items,
    bool NeedsHtmlDependentCompletionItems);
