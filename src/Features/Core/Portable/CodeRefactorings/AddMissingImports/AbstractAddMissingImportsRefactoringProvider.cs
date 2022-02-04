// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal abstract class AbstractAddMissingImportsRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IPasteTrackingService _pasteTrackingService;
        protected abstract string CodeActionTitle { get; }

        public AbstractAddMissingImportsRefactoringProvider(IPasteTrackingService pasteTrackingService)
            => _pasteTrackingService = pasteTrackingService;

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            // Currently this refactoring requires the SourceTextContainer to have a pasted text span.
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (!_pasteTrackingService.TryGetPastedTextSpan(sourceText.Container, out var textSpan))
            {
                return;
            }

            // Check pasted text span for missing imports
            var addMissingImportsService = document.GetLanguageService<IAddMissingImportsFeatureService>();

            var placement = await AddImportPlacementOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            var options = new AddMissingImportsOptions(context.Options.HideAdvancedMembers, placement);

            var analysis = await addMissingImportsService.AnalyzeAsync(document, textSpan, options, cancellationToken).ConfigureAwait(false);
            if (!analysis.CanAddMissingImports)
            {
                return;
            }

            var addImportsCodeAction = new AddMissingImportsCodeAction(
                CodeActionTitle,
                cancellationToken => AddMissingImportsAsync(document, addMissingImportsService, analysis, cancellationToken));

            context.RegisterRefactoring(addImportsCodeAction, textSpan);
        }

        private static async Task<Solution> AddMissingImportsAsync(Document document, IAddMissingImportsFeatureService addMissingImportsService, AddMissingImportsAnalysisResult analysis, CancellationToken cancellationToken)
        {
            var modifiedDocument = await addMissingImportsService.AddMissingImportsAsync(document, analysis, cancellationToken).ConfigureAwait(false);
            return modifiedDocument.Project.Solution;
        }

        private class AddMissingImportsCodeAction : CodeActions.CodeAction.SolutionChangeAction
        {
            public AddMissingImportsCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution, title)
            {
            }
        }
    }
}
