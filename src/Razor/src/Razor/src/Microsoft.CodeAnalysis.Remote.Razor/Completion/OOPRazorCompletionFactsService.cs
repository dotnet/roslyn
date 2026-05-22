// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.Completion;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

[Export(typeof(IRazorCompletionFactsService)), Shared]
[method: ImportingConstructor]
internal sealed class OOPRazorCompletionFactsService([ImportMany] IEnumerable<IRazorCompletionItemProvider> providers)
    : AbstractRazorCompletionFactsService(providers.ToImmutableArray())
{
}
