// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.ServerLifetime;

[ExportCSharpVisualBasicLspServiceFactory(typeof(DidChangeWorkspaceFoldersNotificationHandler)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DidChangeWorkspaceFoldersNotificationHandlerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new DidChangeWorkspaceFoldersNotificationHandler(
            lspServices.GetRequiredService<IWorkspaceFolderTracker>());
}

[Method(Methods.WorkspaceDidChangeWorkspaceFoldersName)]
internal sealed class DidChangeWorkspaceFoldersNotificationHandler(IWorkspaceFolderTracker workspaceFolderTracker)
    : ILspService, ILspServiceNotificationHandler<DidChangeWorkspaceFoldersParams>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    Task INotificationHandler<DidChangeWorkspaceFoldersParams, RequestContext>.HandleNotificationAsync(
        DidChangeWorkspaceFoldersParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var addedFolders = ToFolderPaths(request.Event?.Added);
        var removedFolders = ToFolderPaths(request.Event?.Removed);

        workspaceFolderTracker.Update(addedFolders, removedFolders);

        return Task.CompletedTask;

        static ImmutableArray<string> ToFolderPaths(WorkspaceFolder[]? folders)
        {
            if (folders is null)
                return ImmutableArray<string>.Empty;

            var builder = ImmutableArray.CreateBuilder<string>(folders.Length);
            foreach (var folder in folders)
                builder.Add(folder.DocumentUri.GetDocumentFilePathFromUri());

            return builder.ToImmutable();
        }
    }
}
