// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.CodeLens;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService
    {
        public async Task<ReferenceCount> GetReferenceCountAsync(DocumentId documentId, TextSpan textSpan, int maxResultCount, CancellationToken cancellationToken)
        {
            try
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_GetReferenceCountAsync, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(solution, documentId,
                        syntaxNode, maxResultCount, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                // stream to send over result has closed before we
                // had chance to check cancellation
            }
            catch (OperationCanceledException)
            {
                // rpc connection has closed.
                // this can happen if client side cancelled the
                // operation
            }

            return null;
        }

        public async Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            try
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceLocationsAsync, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(solution, documentId,
                        syntaxNode, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                // stream to send over result has closed before we
                // had chance to check cancellation
            }
            catch (OperationCanceledException)
            {
                // rpc connection has closed.
                // this can happen if client side cancelled the
                // operation
            }

            return null;
        }

        public async Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            try
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceMethodsAsync, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId,
                        syntaxNode, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                // stream to send over result has closed before we
                // had chance to check cancellation
            }
            catch (OperationCanceledException)
            {
                // rpc connection has closed.
                // this can happen if client side cancelled the
                // operation
            }

            return null;
        }

        public async Task<string> GetFullyQualifiedName(DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            try
            {
                using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_GetFullyQualifiedName, documentId.ProjectId.DebugName, cancellationToken))
                {
                    var solution = await GetSolutionAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);

                    return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedName(solution, documentId,
                        syntaxNode, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                // stream to send over result has closed before we
                // had chance to check cancellation
            }
            catch (OperationCanceledException)
            {
                // rpc connection has closed.
                // this can happen if client side cancelled the
                // operation
            }

            return null;
        }
    }
}
