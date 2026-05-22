// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.FoldingRanges;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.CodeAnalysis.Remote.Razor.FoldingRanges;

[Export(typeof(IFoldingRangeService)), Shared]
[method: ImportingConstructor]
internal sealed class OOPFoldingRangeService(
    IDocumentMappingService documentMappingService,
    [ImportMany] IEnumerable<IRazorFoldingRangeProvider> foldingRangeProviders,
    ILoggerFactory loggerFactory)
    : FoldingRangeService(documentMappingService, foldingRangeProviders, loggerFactory)
{
}
