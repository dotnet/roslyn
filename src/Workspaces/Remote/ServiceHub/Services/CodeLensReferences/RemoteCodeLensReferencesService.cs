// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteCodeLensReferencesService : BrokeredServiceBase, IRemoteCodeLensReferencesService
    {
        internal sealed class Factory : FactoryBase<IRemoteCodeLensReferencesService>
        {
            protected override IRemoteCodeLensReferencesService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteCodeLensReferencesService(arguments);
        }

        public RemoteCodeLensReferencesService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        private static async ValueTask<SyntaxNode?> TryFindNodeAsync(Solution solution, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                return null;
            }

            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return syntaxRoot.FindNode(textSpan);
        }

        public ValueTask<ReferenceCount?> GetReferenceCountAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, int maxResultCount, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (Logger.LogBlock(FunctionId.CodeAnalysisService_GetReferenceCountAsync, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var syntaxNode = await TryFindNodeAsync(solution, documentId, textSpan, cancellationToken).ConfigureAwait(false);
                    if (syntaxNode == null)
                    {
                        return null;
                    }

                    return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(
                        solution,
                        documentId,
                        syntaxNode,
                        maxResultCount,
                        cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<ReferenceLocationDescriptor>?> FindReferenceLocationsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceLocationsAsync, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var syntaxNode = await TryFindNodeAsync(solution, documentId, textSpan, cancellationToken).ConfigureAwait(false);
                    if (syntaxNode == null)
                    {
                        return null;
                    }

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(
                        solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public ValueTask<ImmutableArray<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceMethodsAsync, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var syntaxNode = await TryFindNodeAsync(solution, documentId, textSpan, cancellationToken).ConfigureAwait(false);
                    if (syntaxNode == null)
                    {
                        return null;
                    }

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(
                        solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public ValueTask<string?> GetFullyQualifiedNameAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (Logger.LogBlock(FunctionId.CodeAnalysisService_GetFullyQualifiedName, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var syntaxNode = await TryFindNodeAsync(solution, documentId, textSpan, cancellationToken).ConfigureAwait(false);
                    if (syntaxNode == null)
                    {
                        return null;
                    }

                    return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedNameAsync(
                        solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}
