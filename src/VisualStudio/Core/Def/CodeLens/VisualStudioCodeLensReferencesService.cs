// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.CodeLens;

[ExportWorkspaceService(typeof(ICodeLensReferencesService), layer: ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioCodeLensReferencesService() : ICodeLensReferencesService
{
    public ValueTask<VersionStamp> GetProjectCodeLensVersionAsync(Solution solution, ProjectId projectId, CancellationToken cancellationToken)
    {
        // This value is more efficient to calculate in the current process
        return CodeLensReferencesServiceFactory.Instance.GetProjectCodeLensVersionAsync(solution, projectId, cancellationToken);
    }

    public async Task<ReferenceCount?> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode, int maxSearchResults,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CodeLens_GetReferenceCountAsync, cancellationToken))
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteCodeLensReferencesService, ReferenceCount?>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.GetReferenceCountAsync(solutionInfo, documentId, syntaxNode.Span, maxSearchResults, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : null;
            }

            return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(solution, documentId, syntaxNode, maxSearchResults, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ImmutableArray<ReferenceLocationDescriptorAndDocument>?> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CodeLens_FindReferenceLocationsAsync, cancellationToken))
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteCodeLensReferencesService, ImmutableArray<ReferenceLocationDescriptorAndDocument>?>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.FindReferenceLocationsAsync(solutionInfo, documentId, syntaxNode.Span, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : null;
            }

            // remote host is not running. this can happen if remote host is disabled.
            return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<ImmutableArray<ReferenceLocationDescriptor>> MapReferenceLocationsAsync(Solution solution, ImmutableArray<ReferenceLocationDescriptorAndDocument> referenceLocations, ClassificationOptions classificationOptions, CancellationToken cancellationToken)
    {
        // Always do the mapping in-process, since we're replying on in-process document services
        return CodeLensReferencesServiceFactory.Instance.MapReferenceLocationsAsync(solution, referenceLocations, classificationOptions, cancellationToken);
    }

    public async Task<ImmutableArray<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CodeLens_FindReferenceMethodsAsync, cancellationToken))
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteCodeLensReferencesService, ImmutableArray<ReferenceMethodDescriptor>?>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.FindReferenceMethodsAsync(solutionInfo, documentId, syntaxNode.Span, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : null;
            }

            return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string?> GetFullyQualifiedNameAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CodeLens_GetFullyQualifiedName, cancellationToken))
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteCodeLensReferencesService, string?>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.GetFullyQualifiedNameAsync(solutionInfo, documentId, syntaxNode.Span, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : null;
            }

            return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedNameAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
        }
    }
}
