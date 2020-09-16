// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteCodeLensReferencesService
    {
        public Task<ReferenceCount> GetReferenceCountAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, int maxResultCount, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_GetReferenceCountAsync, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        return null;
                    }

                    var syntaxNode = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(
                        solution,
                        documentId,
                        syntaxNode,
                        maxResultCount,
                        cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceLocationsAsync, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        return null;
                    }

                    var syntaxNode = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(
                        solution,
                        documentId,
                        syntaxNode,
                        cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceMethodsAsync, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        return null;
                    }

                    var syntaxNode = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId,
                        syntaxNode, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<string> GetFullyQualifiedNameAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_GetFullyQualifiedName, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        return null;
                    }

                    var syntaxNode = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedNameAsync(solution, documentId,
                        syntaxNode, cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }
    }
}
