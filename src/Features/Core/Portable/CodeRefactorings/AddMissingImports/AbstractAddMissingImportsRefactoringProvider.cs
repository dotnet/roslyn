// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddMissingImports
{
    internal abstract class AbstractAddMissingImportsRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IPasteTrackingService? _pasteTrackingService;
        protected abstract string CodeActionTitle { get; }

        public AbstractAddMissingImportsRefactoringProvider(IPasteTrackingService? pasteTrackingService)
            => _pasteTrackingService = pasteTrackingService;

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // If we aren't in a host that supports paste tracking, we can't do anything. This is just to avoid creating MEF part rejections for
            // things composing the Features layer.
            if (_pasteTrackingService == null)
                return;

            var (document, _, cancellationToken) = context;
            // Currently this refactoring requires the SourceTextContainer to have a pasted text span.
            var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            if (!_pasteTrackingService.TryGetPastedTextSpan(sourceText.Container, out var textSpan))
            {
                return;
            }

            // Check pasted text span for missing imports
            var addMissingImportsService = document.GetRequiredLanguageService<IAddMissingImportsFeatureService>();

            var cleanupOptions = await document.GetCodeCleanupOptionsAsync(context.Options, cancellationToken).ConfigureAwait(false);
            var options = new AddMissingImportsOptions(
                cleanupOptions,
                context.Options.GetOptions(document.Project.Services).HideAdvancedMembers);

            var analysis = await addMissingImportsService.AnalyzeAsync(document, textSpan, options, cancellationToken).ConfigureAwait(false);
            if (!analysis.CanAddMissingImports)
            {
                return;
            }

            var addImportsCodeAction = CodeAction.Create(
                CodeActionTitle,
                cancellationToken => AddMissingImportsAsync(document, addMissingImportsService, analysis, options.CleanupOptions.FormattingOptions, cancellationToken),
                CodeActionTitle);

            context.RegisterRefactoring(addImportsCodeAction, textSpan);
        }

        private static async Task<Solution> AddMissingImportsAsync(Document document, IAddMissingImportsFeatureService addMissingImportsService, AddMissingImportsAnalysisResult analysis, SyntaxFormattingOptions formattingOptions, CancellationToken cancellationToken)
        {
            var modifiedDocument = await addMissingImportsService.AddMissingImportsAsync(document, analysis, formattingOptions, cancellationToken).ConfigureAwait(false);
            return modifiedDocument.Project.Solution;
        }
    }
}
