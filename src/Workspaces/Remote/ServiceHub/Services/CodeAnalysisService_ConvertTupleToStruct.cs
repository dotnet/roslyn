// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteConvertTupleToStructCodeRefactoringProvider
    {
        public Task<SerializableConvertTupleToStructResult> ConvertToStructAsync(
            PinnedSolutionInfo solutionInfo,
            DocumentId documentId,
            TextSpan span,
            Scope scope,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync<SerializableConvertTupleToStructResult>(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    var service = document.GetLanguageService<IConvertTupleToStructCodeRefactoringProvider>();
                    var updatedSolution = await service.ConvertToStructAsync(document, span, scope, cancellationToken).ConfigureAwait(false);

                    var cleanedSolution = await CleanupAsync(solution, updatedSolution, cancellationToken).ConfigureAwait(false);

                    var documentTextChanges = await RemoteUtilities.GetDocumentTextChangesAsync(
                        solution, cleanedSolution, cancellationToken).ConfigureAwait(false);
                    var renamedToken = await GetRenamedTokenAsync(
                        solution, cleanedSolution, cancellationToken).ConfigureAwait(false);

                    return new SerializableConvertTupleToStructResult
                    {
                        DocumentTextChanges = documentTextChanges,
                        RenamedToken = renamedToken,
                    };
                }
            }, cancellationToken);
        }

        private static async Task<(DocumentId, TextSpan)> GetRenamedTokenAsync(
            Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
        {
            var changes = newSolution.GetChangedDocuments(oldSolution);

            foreach (var docId in changes)
            {
                var document = newSolution.GetDocument(docId);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var renamedToken = root.GetAnnotatedTokens(RenameAnnotation.Kind).FirstOrNull();
                if (renamedToken == null)
                    continue;

                return (docId, renamedToken.Value.Span);
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static async Task<Solution> CleanupAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
        {
            var changes = newSolution.GetChangedDocuments(oldSolution);
            var final = newSolution;

            foreach (var docId in changes)
            {
                var cleaned = await CodeAction.CleanupDocumentAsync(
                    newSolution.GetDocument(docId), cancellationToken).ConfigureAwait(false);
                var cleanedRoot = await cleaned.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                final = final.WithDocumentSyntaxRoot(docId, cleanedRoot);
            }

            return final;
        }
    }
}
