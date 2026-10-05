// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;

[ExportCSharpVisualBasicLspService(typeof(DidChangeWorkspaceFoldersNotificationHandler)), Shared(LspServiceComposition.SharingBoundary)]
[Method(Methods.WorkspaceDidChangeWorkspaceFoldersName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DidChangeWorkspaceFoldersNotificationHandler(LspService<IWorkspaceFolderTracker> workspaceFolderTracker)
    : ILspServiceNotificationHandler<DidChangeWorkspaceFoldersParams>
{
    private readonly IWorkspaceFolderTracker _workspaceFolderTracker = workspaceFolderTracker.Value;

    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    Task INotificationHandler<DidChangeWorkspaceFoldersParams, RequestContext>.HandleNotificationAsync(
        DidChangeWorkspaceFoldersParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        _workspaceFolderTracker.Update(
            addedFolders: request.Event?.Added,
            removedFolders: request.Event?.Removed);

        return Task.CompletedTask;
    }
}
