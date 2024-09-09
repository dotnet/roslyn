// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Resolves a code action by filling out its Edit property. The handler is triggered only when a user hovers over a
/// code action. This system allows the basic code action data to be computed quickly, and the complex data, to be
/// computed only when necessary (i.e. when hovering/previewing a code action).
/// <para>
/// This system only supports text edits to documents.  In the future, supporting complex edits (including changes to
/// project files) would be desirable.
/// </para>
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(CodeActionResolveHandler)), Shared]
[Method(LSP.Methods.CodeActionResolveName)]
internal class CodeActionResolveHandler : ILspServiceDocumentRequestHandler<LSP.CodeAction, LSP.CodeAction>
{
    private readonly ICodeFixService _codeFixService;
    private readonly ICodeRefactoringService _codeRefactoringService;
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeActionResolveHandler(
        ICodeFixService codeFixService,
        ICodeRefactoringService codeRefactoringService,
        IGlobalOptionService globalOptions)
    {
        _codeFixService = codeFixService;
        _codeRefactoringService = codeRefactoringService;
        _globalOptions = globalOptions;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.CodeAction request)
        => GetCodeActionResolveData(request).TextDocument;

    public async Task<LSP.CodeAction> HandleRequestAsync(LSP.CodeAction codeAction, RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(codeAction.Data);
        var data = GetCodeActionResolveData(codeAction);
        Assumes.Present(data);

        // Fix All Code Action does not need further resolution since it already has the command callback
        // when the action is initially created.
        if (data.FixAllFlavors is not null)
        {
            return codeAction;
        }

        // We don't need to resolve a top level code action that has nested actions - it requires further action
        // on the client to pick which of the nested actions to actually apply.
        if (data.NestedCodeActions.HasValue && data.NestedCodeActions.Value.Length > 0)
        {
            return codeAction;
        }

        var document = context.GetRequiredTextDocument();
        var solution = document.Project.Solution;
        var codeActions = await CodeActionHelpers.GetCodeActionsAsync(
            document,
            data.Range,
            _codeFixService,
            _codeRefactoringService,
            fixAllScope: null,
            cancellationToken).ConfigureAwait(false);

        Contract.ThrowIfNull(data.CodeActionPath);
        var codeActionToResolve = CodeActionHelpers.GetCodeActionToResolve(data.CodeActionPath, codeActions, isFixAllAction: false);

        // LSP currently has no way to report progress for code action computation.
        var operations = await codeActionToResolve.GetOperationsAsync(
            solution, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);

        var edit = await CodeActionResolveHelper.GetCodeActionResolveEditsAsync(context, data, operations, cancellationToken).ConfigureAwait(false);

        codeAction.Edit = edit;
        return codeAction;
    }

    private static CodeActionResolveData GetCodeActionResolveData(LSP.CodeAction request)
    {
        var resolveData = JsonSerializer.Deserialize<CodeActionResolveData>((JsonElement)request.Data!, ProtocolConversions.LspJsonSerializerOptions);
        Contract.ThrowIfNull(resolveData, "Missing data for code action resolve request");
        return resolveData;
    }
}
