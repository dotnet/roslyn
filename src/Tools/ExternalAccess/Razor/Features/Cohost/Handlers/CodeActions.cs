// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
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

    public static async Task<string> GetFormattedNewFileContentAsync(Document document, CancellationToken cancellationToken)
    {
        var project = document.Project;
        // Run the new document formatting service, to make sure the right namespace type is used, among other things
        var formattingService = document.GetLanguageService<INewDocumentFormattingService>();
        if (formattingService is not null)
        {
            var hintDocument = project.Documents.FirstOrDefault();
            var cleanupOptions = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);
            document = await formattingService.FormatNewDocumentAsync(document, hintDocument, cleanupOptions, cancellationToken).ConfigureAwait(false);
        }

        // Unlike normal new file formatting, Razor also wants to remove unnecessary usings
        var syntaxFormattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var removeImportsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
        if (removeImportsService is not null)
        {
            document = await removeImportsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
        }

        // Now format the document so indentation etc. is correct
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        root = Formatter.Format(root, project.Solution.Services, syntaxFormattingOptions, cancellationToken);

        return root.ToFullString();
    }

    public static async Task<TextEdit[]> GetSimplifiedEditsAsync(Document document, TextEdit textEdit, CancellationToken cancellationToken)
    {
        // Create a temporary syntax tree that includes the text edit.
        var originalSourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var pendingChange = ProtocolConversions.TextEditToTextChange(textEdit, originalSourceText);
        var newSourceText = originalSourceText.WithChanges(pendingChange);
        var originalTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var newTree = originalTree.WithChangedText(newSourceText);

        // Find the node that represents the text edit in the new syntax tree and annotate it for the simplifier.
        // Then create a document with a new syntax tree that has the annotated node.
        var node = newTree.FindNode(pendingChange.Span, findInTrivia: false, getInnermostNodeForTie: false, cancellationToken);
        var annotatedSyntaxRoot = newTree.GetRoot(cancellationToken).ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation));
        var annotatedDocument = document.WithSyntaxRoot(annotatedSyntaxRoot);

        // Call to the Simplifier and pass back the edits.
        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var simplificationService = document.Project.Services.GetRequiredService<ISimplificationService>();
        var options = simplificationService.GetSimplifierOptions(configOptions);
        var newDocument = await Simplifier.ReduceAsync(annotatedDocument, options, cancellationToken).ConfigureAwait(false);
        var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
        return [.. changes.Select(change => ProtocolConversions.TextChangeToTextEdit(change, originalSourceText))];
    }
}
