// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteConvertTupleToStructCodeRefactoringService(in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteConvertTupleToStructCodeRefactoringService
{
    internal sealed class Factory : FactoryBase<IRemoteConvertTupleToStructCodeRefactoringService>
    {
        protected override IRemoteConvertTupleToStructCodeRefactoringService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteConvertTupleToStructCodeRefactoringService(arguments);
    }

    public ValueTask<SerializableConvertTupleToStructResult> ConvertToStructAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        TextSpan span,
        Scope scope,
        bool isRecord,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = solution.GetRequiredDocument(documentId);

            var service = document.GetRequiredLanguageService<IConvertTupleToStructCodeRefactoringProvider>();

            var updatedSolution = await service.ConvertToStructAsync(document, span, scope, isRecord, cancellationToken).ConfigureAwait(false);

            var cleanedSolution = await CleanupAsync(solution, updatedSolution, cancellationToken).ConfigureAwait(false);

            var documentTextChanges = await RemoteUtilities.GetDocumentTextChangesAsync(
                solution, cleanedSolution, cancellationToken).ConfigureAwait(false);
            var renamedToken = await GetRenamedTokenAsync(
                solution, cleanedSolution, cancellationToken).ConfigureAwait(false);

            return new SerializableConvertTupleToStructResult(documentTextChanges, renamedToken);
        }, cancellationToken);
    }

    private static async Task<(DocumentId, TextSpan)> GetRenamedTokenAsync(
        Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
    {
        var changes = newSolution.GetChangedDocuments(oldSolution);

        foreach (var docId in changes)
        {
            var document = newSolution.GetRequiredDocument(docId);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var renamedToken = root.GetAnnotatedTokens(RenameAnnotation.Kind).FirstOrNull();
            if (renamedToken == null)
                continue;

            return (docId, renamedToken.Value.Span);
        }

        throw ExceptionUtilities.Unreachable();
    }

    private static async Task<Solution> CleanupAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
    {
        var changes = newSolution.GetChangedDocuments(oldSolution);
        var final = newSolution;

        var changedDocuments = await ProducerConsumer<(DocumentId documentId, SyntaxNode newRoot)>.RunParallelAsync(
            source: changes,
            produceItems: static async (docId, callback, newSolution, cancellationToken) =>
            {
                var document = newSolution.GetRequiredDocument(docId);

                var options = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);
                var cleaned = await CodeAction.CleanupDocumentAsync(document, options, cancellationToken).ConfigureAwait(false);

                var cleanedRoot = await cleaned.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                callback((docId, cleanedRoot));
            },
            args: newSolution,
            cancellationToken).ConfigureAwait(false);

        return newSolution.WithDocumentSyntaxRoots(changedDocuments);
    }
}
