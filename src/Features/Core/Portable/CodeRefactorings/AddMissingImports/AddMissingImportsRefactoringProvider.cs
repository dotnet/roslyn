// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

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
            var textSpan = TryGetPasteTrackingSpan(document);

            if (textSpan.IsEmpty)
            {
                return;
            }

            var addMissingImportsService = document.GetLanguageService<IAddMissingImportsFeatureService>();

            var isMissingImports = await addMissingImportsService.IsMissingImportsAsync(document, textSpan, context.CancellationToken).ConfigureAwait(false);
            if (!isMissingImports)
            {
                return;
            }

            context.RegisterRefactoring(new AddMissingImportsCodeAction(async (cancellationToken) =>
            {
                var newProject = await addMissingImportsService.AddMissingImportsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                return newProject.Solution;
            }));
        }

        private TextSpan TryGetPasteTrackingSpan(Document document)
        {
            return _pasteTrackingService.TryGetPastedTextSpan(document, out var textSpan)
                ? textSpan
                : default;
        }
    }
}
