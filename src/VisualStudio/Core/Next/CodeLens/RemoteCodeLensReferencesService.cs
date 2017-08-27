// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.CodeLens
{
    [ExportWorkspaceService(typeof(ICodeLensReferencesService), layer: ServiceLayer.Host), Shared]
    internal sealed class RemoteCodeLensReferencesService : ICodeLensReferencesService
    {
        public async Task<ReferenceCount> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, int maxSearchResults,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeLens_GetReferenceCountAsync, cancellationToken))
            {
                if (syntaxNode == null)
                {
                    return null;
                }

                var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (remoteHostClient == null)
                {
                    // remote host is not running. this can happen if remote host is disabled.
                    return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(solution, documentId, syntaxNode, maxSearchResults, cancellationToken).ConfigureAwait(false);
                }

                // TODO: send telemetry on session
                return await remoteHostClient.TryRunCodeAnalysisRemoteAsync<ReferenceCount>(
                    solution, WellKnownServiceHubServices.CodeAnalysisService_GetReferenceCountAsync,
                    new object[] { documentId, syntaxNode.Span, maxSearchResults }, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeLens_FindReferenceLocationsAsync, cancellationToken))
            {
                if (syntaxNode == null)
                {
                    return null;
                }

                var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (remoteHostClient == null)
                {
                    // remote host is not running. this can happen if remote host is disabled.
                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }

                // TODO: send telemetry on session
                return await remoteHostClient.TryRunCodeAnalysisRemoteAsync<IEnumerable<ReferenceLocationDescriptor>>(
                    solution, WellKnownServiceHubServices.CodeAnalysisService_FindReferenceLocationsAsync,
                    new object[] { documentId, syntaxNode.Span }, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeLens_FindReferenceMethodsAsync, cancellationToken))
            {
                if (syntaxNode == null)
                {
                    return null;
                }

                var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (remoteHostClient == null)
                {
                    // remote host is not running. this can happen if remote host is disabled.
                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }

                // TODO: send telemetry on session
                return await remoteHostClient.TryRunCodeAnalysisRemoteAsync<IEnumerable<ReferenceMethodDescriptor>>(
                    solution, WellKnownServiceHubServices.CodeAnalysisService_FindReferenceMethodsAsync,
                    new object[] { documentId, syntaxNode.Span }, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<string> GetFullyQualifiedName(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeLens_GetFullyQualifiedName, cancellationToken))
            {
                if (syntaxNode == null)
                {
                    return null;
                }

                var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (remoteHostClient == null)
                {
                    // remote host is not running. this can happen if remote host is disabled.
                    return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedName(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }

                // TODO: send telemetry on session
                return await remoteHostClient.TryRunCodeAnalysisRemoteAsync<string>(
                    solution, WellKnownServiceHubServices.CodeAnalysisService_GetFullyQualifiedName,
                    new object[] { documentId, syntaxNode.Span }, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
