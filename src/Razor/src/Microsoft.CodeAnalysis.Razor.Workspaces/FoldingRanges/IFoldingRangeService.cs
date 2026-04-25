// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.FoldingRanges;

internal interface IFoldingRangeService
{
    ImmutableArray<FoldingRange> GetFoldingRanges(RazorCodeDocument codeDocument, FoldingRange[] csharpRanges, ImmutableArray<FoldingRange> htmlRanges, CancellationToken cancellationToken);
}
