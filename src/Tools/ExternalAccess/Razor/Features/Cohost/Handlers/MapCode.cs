// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class MapCode
{
    public static Task<WorkspaceEdit?> GetMappedWorkspaceEditAsync(
        RazorCohostRequestContext? context,
        Solution solution,
        VSInternalMapCodeMapping[] mappings,
        bool supportsDocumentChanges,
        CancellationToken cancellationToken)
    {
         // Razor can't construct a RazorCohostRequestContext so we need to handle the null case, for their tests
        var logger = context is { } razorContext ? razorContext.GetRequiredService<ILspLogger>() : NoOpLspLogger.Instance;
        return MapCodeHandler.GetMappedWorkspaceEditAsync(solution, mappings, supportsDocumentChanges, logger, cancellationToken);
    }
}
