// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        private async Task<Solution> FixAllAsync(
            FixAllContext fixAllContext,
            IProgress<CodeAnalysisProgress> progress,
            CancellationToken cancellationToken)
        {

        }
    }
}
