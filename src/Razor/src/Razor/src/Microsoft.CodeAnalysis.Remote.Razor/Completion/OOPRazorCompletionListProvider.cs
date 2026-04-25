// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

[Export(typeof(RazorCompletionListProvider)), Shared]
[method: ImportingConstructor]
internal sealed class OOPRazorCompletionListProvider(
    IRazorCompletionFactsService completionFactsService,
    CompletionListCache completionListCache,
    ILoggerFactory loggerFactory)
    : RazorCompletionListProvider(completionFactsService, completionListCache, loggerFactory)
{
}
