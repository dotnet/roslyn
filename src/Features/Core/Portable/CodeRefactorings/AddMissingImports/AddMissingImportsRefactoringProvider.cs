// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

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

            if (!_pasteTrackingService.TryGetPasteTrackingInformation(document, out var trackingInformation)
                || !context.Span.IntersectsWith(trackingInformation.TextSpan))
            {
                return;
            }

            var addMissingImportsService = document.GetLanguageService<IAddMissingImportsFeatureService>();

            var isMissingImports = await addMissingImportsService.IsMissingImportsAsync(document, trackingInformation.TextSpan, context.CancellationToken).ConfigureAwait(false);
            if (!isMissingImports)
            {
                return;
            }

            context.RegisterRefactoring(new SolutionChangeAction(FeaturesResources.Add_missing_imports_for_pasted_code, async (cancellationToken) =>
            {
                var newProject = await addMissingImportsService.AddMissingImportsAsync(document, trackingInformation.TextSpan, context.CancellationToken).ConfigureAwait(false);
                return newProject.Solution;
            }));
        }
    }
}
