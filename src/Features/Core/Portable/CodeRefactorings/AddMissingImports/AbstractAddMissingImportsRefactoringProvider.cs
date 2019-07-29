// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal abstract class AbstractAddMissingImportsRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IPasteTrackingService _pasteTrackingService;
        protected abstract string CodeActionTitle { get; }

        public AbstractAddMissingImportsRefactoringProvider(IPasteTrackingService pasteTrackingService)
        {
            _pasteTrackingService = pasteTrackingService;
        }

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
            var hasMissingImports = await addMissingImportsService.HasMissingImportsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

            if (!hasMissingImports)
            {
                return;
            }

            var addImportsCodeAction = new AddMissingImportsCodeAction(
                CodeActionTitle,
                cancellationToken => AddMissingImports(document, textSpan, cancellationToken));
            context.RegisterRefactoring(addImportsCodeAction);
        }

        private async Task<Solution> AddMissingImports(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            // Add missing imports for the pasted text span.
            var addMissingImportsService = document.GetLanguageService<IAddMissingImportsFeatureService>();
            var newProject = await addMissingImportsService.AddMissingImportsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            return newProject.Solution;
        }

        private class AddMissingImportsCodeAction : CodeActions.CodeAction.SolutionChangeAction
        {
            public AddMissingImportsCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}
