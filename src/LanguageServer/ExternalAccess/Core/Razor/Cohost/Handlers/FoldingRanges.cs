// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Razor.Cohost.Handlers;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;
#endif

internal static class FoldingRanges
{
    public static Task<FoldingRange[]> GetFoldingRangesAsync(Document document, bool lineFoldingOnly, CancellationToken cancellationToken)
    {
        // We need to manually get the IGlobalOptionsService out of the Mef composition, because Razor has its own
        // composition so can't import it (and its internal anyway)
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();

        return FoldingRangesHandler.GetFoldingRangesAsync(globalOptions, document, lineFoldingOnly: lineFoldingOnly, cancellationToken);
    }
}
