// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Remote;

namespace Microsoft.VisualStudio.LanguageServices.CodeLens
{
    [ExportWorkspaceService(typeof(ICodeLensReferencesService), layer: ServiceLayer.Host), Shared]
    internal sealed class RemoteCodeLensReferencesService : ICodeLensReferencesService
    {
        public async Task<ReferenceCount> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, int maxSearchResults,
            CancellationToken cancellationToken)
        {
            var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHostClient == null)
            {
                // remote host is not running. this can happen if remote host is disabled.
                return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(solution, documentId, syntaxNode, maxSearchResults, cancellationToken).ConfigureAwait(false);
            }

            // TODO: send telemetry on session
            using (var session = await remoteHostClient.CreateCodeAnalysisServiceSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                return await session.InvokeAsync<ReferenceCount>(WellKnownServiceHubServices.CodeAnalysisService_GetReferenceCountAsync, new CodeLensArguments(documentId, syntaxNode), maxSearchResults).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHostClient == null)
            {
                // remote host is not running. this can happen if remote host is disabled.
                return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
            }

            // TODO: send telemetry on session
            using (var session = await remoteHostClient.CreateCodeAnalysisServiceSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                return await session.InvokeAsync<IEnumerable<ReferenceLocationDescriptor>>(WellKnownServiceHubServices.CodeAnalysisService_FindReferenceLocationsAsync, new CodeLensArguments(documentId, syntaxNode)).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHostClient == null)
            {
                // remote host is not running. this can happen if remote host is disabled.
                return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
            }

            // TODO: send telemetry on session
            using (var session = await remoteHostClient.CreateCodeAnalysisServiceSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                return await session.InvokeAsync<IEnumerable<ReferenceMethodDescriptor>>(WellKnownServiceHubServices.CodeAnalysisService_FindReferenceMethodsAsync, new CodeLensArguments(documentId, syntaxNode)).ConfigureAwait(false);
            }
        }

        public async Task<string> GetFullyQualifiedName(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHostClient == null)
            {
                // remote host is not running. this can happen if remote host is disabled.
                return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedName(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
            }

            // TODO: send telemetry on session
            using (var session = await remoteHostClient.CreateCodeAnalysisServiceSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                return await session.InvokeAsync<string>(WellKnownServiceHubServices.CodeAnalysisService_GetFullyQualifiedName, new CodeLensArguments(documentId, syntaxNode)).ConfigureAwait(false);
            }
        }
    }
}
