// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class FoldingRanges
{
    [Obsolete("Use GetFoldingRangesAsync(Document, bool, CancellationToken) instead", error: true)]
    public static Task<FoldingRange[]> GetFoldingRangesAsync(Document document, CancellationToken cancellationToken)
    {
        return GetFoldingRangesAsync(document, lineFoldingOnly: false, cancellationToken);
    }

    public static Task<FoldingRange[]> GetFoldingRangesAsync(Document document, bool lineFoldingOnly, CancellationToken cancellationToken)
    {
        // We need to manually get the IGlobalOptionsService out of the Mef composition, because Razor has its own
        // composition so can't import it (and its internal anyway)
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();

        return FoldingRangesHandler.GetFoldingRangesAsync(globalOptions, document, lineFoldingOnly: lineFoldingOnly, cancellationToken);
    }
}
