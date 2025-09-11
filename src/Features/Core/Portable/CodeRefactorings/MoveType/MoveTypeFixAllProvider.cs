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
            => [RefactorAllScope.Project, RefactorAllScope.Solution];

        public override Task<CodeAction?> GetRefactoringAsync(RefactorAllContext refactorAllContext)
        {
            if (refactorAllContext.CodeActionEquivalenceKey != MoveTypeOperationKind.RenameFile.ToString())
                return SpecializedTasks.Null<CodeAction>();

            return CodeAction.Create(
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
                                if (document.Project.Documents.Any(
                                        static (d, args) => d.Folders.SequenceEqual(args.document.Folders) && d.Name == args.name,
                                        arg: (document, name)))
                                {
                                    continue;
                                }

                                callback((document.Id, name));
                            }
                        },
                        args: default(VoidResult),
                        cancellationToken).ConfigureAwait(false);

                    var currentSolution = refactorAllContext.Solution;
                    foreach (var group in documentIdsAndNames.GroupBy(static t => t.documentId.ProjectId))
                    {
                        var project = refactorAllContext.Solution.GetRequiredProject(group.Key);
                        var newProject = project;
                        foreach (var (documentId, newFileName) in group)
                        {
                            var document = project.GetRequiredDocument(documentId);
                            newProject = newProject.WithDocumentName(documentId, newFileName);
                        }
                        refactorAllContext = refactorAllContext.With(
                            (document: null, project: newProject), cancellationToken: cancellationToken);
                    }

                    return currentSolution;
                });
        }
    }
}
