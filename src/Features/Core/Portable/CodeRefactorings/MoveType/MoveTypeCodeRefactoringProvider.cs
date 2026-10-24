// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
    Name = PredefinedCodeRefactoringProviderNames.MoveTypeToFile), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class MoveTypeCodeRefactoringProvider() : CodeRefactoringProvider
{
    internal override FixAllProvider? GetFixAllProvider()
        => new MoveTypeFixAllProvider();

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        if (document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles)
            return;

        if (document.IsGeneratedCode(cancellationToken))
            return;

        var service = document.GetRequiredLanguageService<IMoveTypeService>();
        var actions = await service.GetRefactoringAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        context.RegisterRefactorings(actions);
    }

    private sealed class MoveTypeFixAllProvider : FixAllProvider
    {
        public override IEnumerable<CodeFixes.FixAllScope> GetSupportedFixAllScopes()
            => [CodeFixes.FixAllScope.Solution, CodeFixes.FixAllScope.Project];

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            // Currently, we only support bulk fixing up files to match the type within.
            if (fixAllContext.CodeActionEquivalenceKey != MoveTypeOperationKind.RenameFile.ToString())
                return SpecializedTasks.Null<CodeAction>();

            var title = fixAllContext.Scope is CodeFixes.FixAllScope.Project
                ? string.Format(FeaturesResources.Rename_all_files_in_0_to_match_types, fixAllContext.Project.Name)
                : FeaturesResources.Rename_all_files_in_solution_to_match_types;
            return Task.FromResult<CodeAction?>(CodeAction.SolutionChangeAction.New(
                title,
                (progress, cancellationToken) => FixAllAsync(fixAllContext, progress, cancellationToken),
                fixAllContext.CodeActionEquivalenceKey));
        }

        private static async Task<Solution> FixAllAsync(
            FixAllContext fixAllContext,
            IProgress<CodeAnalysisProgress> progress,
            CancellationToken cancellationToken)
        {
            var projects = fixAllContext.Scope is CodeFixes.FixAllScope.Project
                ? ([fixAllContext.Project])
                : fixAllContext.Solution.Projects.Where(p => p.GetLanguageService<IMoveTypeService>() != null);
            var documents = projects.SelectManyAsArray(p => p.Documents);

            // Set the progress bar to be the number of documents we have to process.
            progress.AddItems(documents.Length);

            var documentIdsAndFileNames = await ProducerConsumer<(DocumentId documentId, string name)>.RunParallelAsync(
                documents,
                static async (document, callback, args, cancellationToken) =>
                {
                    var (fixAllContext, progress) = args;

                    // Ensure we update progress as we process each document.
                    using var _ = progress.ItemCompletedScope();

                    var moveTypeService = document.GetRequiredLanguageService<IMoveTypeService>();
                    var name = await moveTypeService.GetDesiredDocumentNameAsync(document, cancellationToken).ConfigureAwait(false);
                    if (name is null)
                        return;

                    callback((document.Id, name));
                },
                args: (fixAllContext, progress),
                cancellationToken).ConfigureAwait(false);

            using var _ = PooledHashSet<string>.GetInstance(out var seenFilePaths);

            var currentSolution = fixAllContext.Solution;
            foreach (var (documentId, name) in documentIdsAndFileNames)
            {
                var document = currentSolution.GetRequiredDocument(documentId);
                if (document.FilePath is null)
                    continue;

                // There are linked files with the same file path.  Ensure we only ever process such a file once.
                if (seenFilePaths.Add(document.FilePath))
                    currentSolution = currentSolution.WithDocumentName(documentId, name);
            }

            progress.Report(CodeAnalysisProgress.Clear());
            return currentSolution;
        }
    }
}
