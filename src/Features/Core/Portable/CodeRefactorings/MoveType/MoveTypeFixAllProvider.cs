// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal sealed partial class MoveTypeCodeRefactoringProvider
{
    private sealed class MoveTypeFixAllProvider : RefactorAllProvider
    {
        public override IEnumerable<RefactorAllScope> GetSupportedRefactorAllScopes()
            => [RefactorAllScope.Project];

        internal override bool ShowPreviewChangesDialog
            => false;

        public override Task<CodeAction?> GetRefactoringAsync(RefactorAllContext refactorAllContext)
        {
            if (refactorAllContext.CodeActionEquivalenceKey != MoveTypeOperationKind.RenameFile.ToString())
                return SpecializedTasks.Null<CodeAction>();

            var codeAction = CodeAction.Create(
                refactorAllContext.GetDefaultRefactorAllTitle(),
                async cancellationToken =>
                {
                    var documentsToCheck = await refactorAllContext.GetRefactorAllSpansAsync(cancellationToken).ConfigureAwait(false);

                    var documentIdsAndNames = await ProducerConsumer<(DocumentId documentId, string newFileName)>.RunParallelAsync(
                        documentsToCheck.Keys,
                        produceItems: static async (document, callback, args, cancellationToken) =>
                        {
                            var service = document.GetRequiredLanguageService<IMoveTypeService>();
                            var suggestedNames = await service.TryGetSuggestedFileRenamesAsync(document, cancellationToken).ConfigureAwait(false);
                            if (suggestedNames.IsEmpty)
                                return;

                            foreach (var name in suggestedNames)
                            {
                                // Ensure the new name isn't one that will conflict with an existing document.
                                if (CollidesWithExistingDocument(document.Project.State, document.State, name))
                                    continue;

                                callback((document.Id, name));
                            }
                        },
                        args: default(VoidResult),
                        cancellationToken).ConfigureAwait(false);

                    var currentSolution = refactorAllContext.Solution;
                    foreach (var (documentId, newFileName) in documentIdsAndNames)
                    {
                        var projectState = currentSolution.GetRequiredProjectState(documentId.ProjectId);
                        var documentState = projectState.DocumentStates.GetRequiredState(documentId);
                        if (CollidesWithExistingDocument(projectState, documentState, newFileName))
                            continue;

                        currentSolution = currentSolution.WithDocumentName(documentId, newFileName);
                    }

                    return currentSolution;
                });

            return Task.FromResult<CodeAction?>(codeAction);
        }

        private static bool CollidesWithExistingDocument(ProjectState projectState, TextDocumentState document, string newName)
        {
            return projectState.DocumentStates.States.Any(
                static (kvp, args) => kvp.Value.Name == args.newName && kvp.Value.Folders.SequenceEqual(args.document.Folders),
                arg: (document, newName));
        }
    }
}
