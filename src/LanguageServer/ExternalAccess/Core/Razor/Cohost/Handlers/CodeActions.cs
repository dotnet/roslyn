// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class CodeActions
{
    public static Task<CodeAction[]> GetCodeActionsAsync(
        Document document,
        CodeActionParams request,
        bool supportsVSExtensions,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;

        var codeFixService = solution.Services.ExportProvider.GetService<ICodeFixService>();
        var codeRefactoringService = solution.Services.ExportProvider.GetService<ICodeRefactoringService>();

        return CodeActionHelpers.GetVSCodeActionsAsync(request, document, codeFixService, codeRefactoringService, supportsVSExtensions, cancellationToken);
    }

    public static async Task<CodeAction> ResolveCodeActionAsync(Document document, CodeAction codeAction, ResourceOperationKind[] resourceOperations, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(codeAction.Data);
        var data = CodeActionResolveHandler.GetCodeActionResolveData(codeAction);
        Assumes.Present(data);

        // We don't need to resolve a top level code action that has nested actions - it requires further action
        // on the client to pick which of the nested actions to actually apply.
        if (data.NestedCodeActions.HasValue && data.NestedCodeActions.Value.Length > 0)
        {
            return codeAction;
        }

        var solution = document.Project.Solution;

        var codeFixService = solution.Services.ExportProvider.GetService<ICodeFixService>();
        var codeRefactoringService = solution.Services.ExportProvider.GetService<ICodeRefactoringService>();

        var codeActions = await CodeActionHelpers.GetCodeActionsAsync(
            document,
            data.Range,
            codeFixService,
            codeRefactoringService,
            fixAllScope: null,
            cancellationToken).ConfigureAwait(false);

        Contract.ThrowIfNull(data.CodeActionPath);
        var codeActionToResolve = CodeActionHelpers.GetCodeActionToResolve(data.CodeActionPath, codeActions, isFixAllAction: false);

        var operations = await codeActionToResolve.GetOperationsAsync(solution, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);

        var edit = await CodeActionResolveHelper.GetCodeActionResolveEditsAsync(
            solution,
            data,
            operations,
            resourceOperations,
            logFunction: static s => { },
            cancellationToken).ConfigureAwait(false);

        codeAction.Edit = edit;
        return codeAction;
    }

    public static Task<string> GetFormattedNewFileContentAsync(Document document, CancellationToken cancellationToken)
        => FormatNewFileHandler.GetFormattedNewFileContentAsync(document, cancellationToken);

    public static Task<TextEdit[]> GetSimplifiedEditsAsync(Document document, TextEdit textEdit, CancellationToken cancellationToken)
        => SimplifyMethodHandler.GetSimplifiedEditsAsync(document, textEdit, cancellationToken);
}
