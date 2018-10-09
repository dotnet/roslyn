// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
            var document = context.Document;

            // Currently this refactoring requires the SourceTextContainer to have a pasted text span.
            var sourceText = await document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            if (!_pasteTrackingService.TryGetPastedTextSpan(sourceText.Container, out var textSpan))
            {
                return;
            }

            // Add missing imports for the pasted text span.
            var addMissingImportsService = document.GetLanguageService<IAddMissingImportsFeatureService>();
            var newProject = await addMissingImportsService.AddMissingImportsAsync(document, textSpan, context.CancellationToken).ConfigureAwait(false);

            // If the project is unchanged, then do not offer the refactoring.
            if (document.Project == newProject)
            {
                return;
            }

            context.RegisterRefactoring(new AddMissingImportsCodeAction(CodeActionTitle, _ => Task.FromResult(newProject.Solution)));
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
