// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeRefactoringProviderNames.AddMissingImports), Shared]
    internal class AddMissingImportsRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IPasteTrackingService _pasteTrackingService;

        [ImportingConstructor]
        public AddMissingImportsRefactoringProvider(IPasteTrackingService pasteTrackingService)
        {
            _pasteTrackingService = pasteTrackingService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;

            // Currently this refactoring requires the document to have a pasted text span.
            if (!_pasteTrackingService.TryGetPastedTextSpan(document, out var textSpan))
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

            var title = document.Project.Language == LanguageNames.CSharp
                ? FeaturesResources.Add_missing_usings
                : FeaturesResources.Add_missing_imports;

            context.RegisterRefactoring(new AddMissingImportsCodeAction(title, _ => Task.FromResult(newProject.Solution)));
        }
    }
}
