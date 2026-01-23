// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

internal abstract class AbstractRazorWillRenameListener : ILspWillRenameListener
{
    Task<WorkspaceEdit?> ILspWillRenameListener.HandleWillRenameAsync(RenameFilesParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var razorRequestContext = new RazorCohostRequestContext(context);
        return HandleRequestAsync(request, razorRequestContext, cancellationToken);
    }

    protected abstract Task<WorkspaceEdit?> HandleRequestAsync(RenameFilesParams request, RazorCohostRequestContext razorRequestContext, CancellationToken cancellationToken);
}
