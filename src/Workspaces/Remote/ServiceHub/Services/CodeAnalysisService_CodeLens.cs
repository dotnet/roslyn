// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeLens;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService
    {
        public async Task<ReferenceCount> GetReferenceCountAsync(DocumentId documentId, TextSpan textSpan, int maxResultCount, byte[] solutionChecksum)
        {
            using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_GetReferenceCountAsync, documentId.ProjectId.DebugName, CancellationToken))
            {
                var solution = await RoslynServices.SolutionService.GetSolutionAsync(new Checksum(solutionChecksum), CancellationToken).ConfigureAwait(false);
                var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);

                return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(solution, documentId,
                    syntaxNode, maxResultCount, CancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(DocumentId documentId, TextSpan textSpan, byte[] solutionChecksum)
        {
            using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceLocationsAsync, documentId.ProjectId.DebugName, CancellationToken))
            {
                var solution = await RoslynServices.SolutionService.GetSolutionAsync(new Checksum(solutionChecksum), CancellationToken).ConfigureAwait(false);
                var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);

                return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(solution, documentId,
                    syntaxNode, CancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(DocumentId documentId, TextSpan textSpan, byte[] solutionChecksum)
        {
            using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_FindReferenceMethodsAsync, documentId.ProjectId.DebugName, CancellationToken))
            {
                var solution = await RoslynServices.SolutionService.GetSolutionAsync(new Checksum(solutionChecksum), CancellationToken).ConfigureAwait(false);
                var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);

                return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId,
                    syntaxNode, CancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<string> GetFullyQualifiedName(DocumentId documentId, TextSpan textSpan, byte[] solutionChecksum)
        {
            using (Internal.Log.Logger.LogBlock(FunctionId.CodeAnalysisService_GetFullyQualifiedName, documentId.ProjectId.DebugName, CancellationToken))
            {
                var solution = await RoslynServices.SolutionService.GetSolutionAsync(new Checksum(solutionChecksum), CancellationToken).ConfigureAwait(false);
                var syntaxNode = (await solution.GetDocument(documentId).GetSyntaxRootAsync().ConfigureAwait(false)).FindNode(textSpan);

                return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedName(solution, documentId,
                    syntaxNode, CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
