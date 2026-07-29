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
            lspServices.GetRequiredService<IInitializeManager>());
}

[Method(Methods.WorkspaceDidChangeWorkspaceFoldersName)]
internal sealed class DidChangeWorkspaceFoldersNotificationHandler(IInitializeManager initializeManager)
    : ILspService, ILspServiceNotificationHandler<DidChangeWorkspaceFoldersParams>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    Task INotificationHandler<DidChangeWorkspaceFoldersParams, RequestContext>.HandleNotificationAsync(
        DidChangeWorkspaceFoldersParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        // Extract folder paths from the change event
        var addedFolders = ImmutableArray<string>.Empty;
        if (request.Event?.Added is not null)
        {
            var builder = ImmutableArray.CreateBuilder<string>(request.Event.Added.Length);
            foreach (var folder in request.Event.Added)
            {
                var folderPath = folder.DocumentUri.GetDocumentFilePathFromUri();
                builder.Add(folderPath);
            }
            addedFolders = builder.ToImmutable();
        }

        var removedFolders = ImmutableArray<string>.Empty;
        if (request.Event?.Removed is not null)
        {
            var builder = ImmutableArray.CreateBuilder<string>(request.Event.Removed.Length);
            foreach (var folder in request.Event.Removed)
            {
                var folderPath = folder.DocumentUri.GetDocumentFilePathFromUri();
                builder.Add(folderPath);
            }
            removedFolders = builder.ToImmutable();
        }

        // Update the initialize manager with new workspace folder information.
        // Subscribers (like WorkspaceProjectDiscoveryService) will be notified via the WorkspaceFoldersChanged event.
        initializeManager.UpdateWorkspaceFolders(addedFolders, removedFolders);

        return Task.CompletedTask;
    }
}
