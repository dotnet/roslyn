// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal sealed class LspRazorCompletionFactsService(IEnumerable<IRazorCompletionItemProvider> providers)
    : AbstractRazorCompletionFactsService(providers.ToImmutableArray())
{
}
