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

            if (!_pasteTrackingService.TryGetPastedTextSpan(document, out var textSpan))
            {
                return;
            }

            var addMissingImportsService = document.GetLanguageService<IAddMissingImportsFeatureService>();
            var newProject = await addMissingImportsService.AddMissingImportsAsync(document, textSpan, context.CancellationToken).ConfigureAwait(false);

            if (document.Project == newProject)
            {
                return;
            }

            context.RegisterRefactoring(new AddMissingImportsCodeAction(_ => Task.FromResult(newProject.Solution)));
        }
    }
}
