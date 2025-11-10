// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PasteTracking;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddMissingImports;

internal abstract class AbstractAddMissingImportsRefactoringProvider : CodeRefactoringProvider
{
    protected abstract string CodeActionTitle { get; }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;

        // If we aren't in a host that supports paste tracking (known by having exactly one export of type
        // IPasteTrackingService), we can't do anything. This is just to avoid creating MEF part rejections for
        // things composing the Features layer.
        var services = document.Project.Solution.Workspace.Services.HostServices as IMefHostExportProvider;
        var pasteTrackingService = services?.GetExports<IPasteTrackingService>().SingleOrDefault()?.Value;
        if (pasteTrackingService is null)
            return;

        // Currently this refactoring requires the SourceTextContainer to have a pasted text span.
        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        if (!pasteTrackingService.TryGetPastedTextSpan(sourceText.Container, out var textSpan))
            return;

        // Check pasted text span for missing imports
        var addMissingImportsService = document.GetRequiredLanguageService<IAddMissingImportsFeatureService>();

        var fixData = await addMissingImportsService.AnalyzeAsync(
            document, textSpan, cleanupDocument: true, cancellationToken).ConfigureAwait(false);
        if (fixData.IsDefaultOrEmpty)
            return;

        var addImportsCodeAction = CodeAction.Create(
            CodeActionTitle,
            (progressTracker, cancellationToken) =>
                addMissingImportsService.AddMissingImportsAsync(document, fixData, progressTracker, cancellationToken),
            CodeActionTitle);

        context.RegisterRefactoring(addImportsCodeAction, textSpan);
    }
}
