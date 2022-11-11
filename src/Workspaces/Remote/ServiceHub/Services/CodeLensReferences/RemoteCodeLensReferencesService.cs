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
            var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            if (document == null)
            {
                return null;
            }

            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Pass getInnermostNodeForTie so top-level statements that are contained within a GlobalStatementSyntax picks the actual
            // definition and not just the GlobalStatementSyntax.
            return syntaxRoot.FindNode(textSpan, getInnermostNodeForTie: true);
        }

        public async ValueTask<ReferenceCount?> GetReferenceCountAsync(Checksum solutionChecksum, DocumentId documentId, TextSpan textSpan, int maxResultCount, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeAnalysisService_GetReferenceCountAsync, documentId.ProjectId.DebugName, cancellationToken))
            {
                return await RunServiceAsync(solutionChecksum, async solution =>
                    {
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
                    },
                    cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask<ImmutableArray<ReferenceLocationDescriptor>?> FindReferenceLocationsAsync(Checksum solutionChecksum, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceLocationsAsync, documentId.ProjectId.DebugName, cancellationToken))
            {
                return await RunServiceAsync(solutionChecksum, async solution =>
                {
                    var syntaxNode = await TryFindNodeAsync(solution, documentId, textSpan, cancellationToken).ConfigureAwait(false);
                    if (syntaxNode == null)
                    {
                        return null;
                    }

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(
                        solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask<ImmutableArray<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(Checksum solutionChecksum, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceMethodsAsync, documentId.ProjectId.DebugName, cancellationToken))
            {
                return await RunServiceAsync(solutionChecksum, async solution =>
                {
                    var syntaxNode = await TryFindNodeAsync(solution, documentId, textSpan, cancellationToken).ConfigureAwait(false);
                    if (syntaxNode == null)
                    {
                        return null;
                    }

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(
                        solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        public ValueTask<string?> GetFullyQualifiedNameAsync(Checksum solutionChecksum, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (Logger.LogBlock(FunctionId.CodeAnalysisService_GetFullyQualifiedName, documentId.ProjectId.DebugName, cancellationToken))
                {
                    return await RunServiceAsync(solutionChecksum, async solution =>
                    {
                        var syntaxNode = await TryFindNodeAsync(solution, documentId, textSpan, cancellationToken).ConfigureAwait(false);
                        if (syntaxNode == null)
                        {
                            return null;
                        }

                        return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedNameAsync(
                            solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                    }, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}
