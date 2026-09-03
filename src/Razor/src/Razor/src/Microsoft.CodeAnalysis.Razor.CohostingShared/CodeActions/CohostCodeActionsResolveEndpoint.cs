// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.CodeActionResolveName)]
[ExportRazorStatelessLspService(typeof(CohostCodeActionsResolveEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostCodeActionsResolveEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IClientCapabilitiesService clientCapabilitiesService,
    IClientSettingsManager clientSettingsManager,
    IHtmlRequestInvoker requestInvoker)
    : AbstractCohostDocumentEndpoint<CodeAction, CodeAction?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(CodeAction request)
    {
        var resolveParams = RazorCodeActionResolutionParams.Unwrap(request);
        return resolveParams.TextDocument;
    }

    protected override Task<CodeAction?> HandleRequestAsync(CodeAction request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var csharpSyntaxFormattingOptions = CSharpFormattingOptionsHelper.GetCSharpSyntaxFormattingOptions(razorDocument, cancellationToken);
        return HandleRequestAsync(request, razorDocument, csharpSyntaxFormattingOptions, cancellationToken);
    }

    private async Task<CodeAction?> HandleRequestAsync(
        CodeAction request,
        TextDocument razorDocument,
        CSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions,
        CancellationToken cancellationToken)
    {
        var resolveParams = RazorCodeActionResolutionParams.Unwrap(request);

        var resolvedDelegatedCodeAction = resolveParams.Language switch
        {
            RazorLanguageKind.Html => await ResolvedHtmlCodeActionAsync(razorDocument, request, resolveParams, cancellationToken).ConfigureAwait(false),
            RazorLanguageKind.CSharp => await ResolveCSharpCodeActionAsync(razorDocument, request, resolveParams, cancellationToken).ConfigureAwait(false),
            _ => null
        };

        var formattingOptions = _clientSettingsManager.GetClientSettings().ToRazorFormattingOptions() with
        {
            CSharpSyntaxFormattingOptions = csharpSyntaxFormattingOptions,
        };

        return await _remoteServiceInvoker.TryInvokeAsync<IRemoteCodeActionsService, CodeAction>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.ResolveCodeActionAsync(solutionInfo, razorDocument.Id, request, resolvedDelegatedCodeAction, formattingOptions, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<CodeAction> ResolveCSharpCodeActionAsync(TextDocument razorDocument, CodeAction codeAction, RazorCodeActionResolutionParams resolveParams, CancellationToken cancellationToken)
    {
        var originalData = codeAction.Data;
        try
        {
            codeAction.Data = resolveParams.Data;

            var uri = resolveParams.DelegatedDocumentUri.AssumeNotNull();

            var generatedDocument = await razorDocument.Project.Solution.TryGetSourceGeneratedDocumentAsync(uri, cancellationToken).ConfigureAwait(false);
            if (generatedDocument is null)
            {
                return codeAction;
            }

            var resourceOptions = _clientCapabilitiesService.ClientCapabilities.Workspace?.WorkspaceEdit?.ResourceOperations ?? [];

            // HACK: VS doesn't advertise resource operations, but it does support some, so we polyfill for it so Roslyn will return the actions
            // it needs to. Can be removed when https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_git/VSLanguageServerClient/pullrequest/734356 is merged.
            if (_clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions)
            {
                resourceOptions = [ResourceOperationKind.Create, ResourceOperationKind.Rename];
            }

            return await ResolveCodeActionAsync(generatedDocument, codeAction, resourceOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            codeAction.Data = originalData;
        }
    }

    private static async Task<CodeAction> ResolveCodeActionAsync(Document document, CodeAction codeAction, ResourceOperationKind[] resourceOperations, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(codeAction.Data);
        var data = CodeActionResolveHandler.GetCodeActionResolveData(codeAction).AssumeNotNull();

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

    private async Task<CodeAction> ResolvedHtmlCodeActionAsync(TextDocument razorDocument, CodeAction codeAction, RazorCodeActionResolutionParams resolveParams, CancellationToken cancellationToken)
    {
        var originalData = codeAction.Data;
        codeAction.Data = resolveParams.Data;

        try
        {
            var result = await _requestInvoker.MakeHtmlLspRequestAsync<CodeAction, CodeAction>(
                razorDocument,
                Methods.CodeActionResolveName,
                codeAction,
                cancellationToken).ConfigureAwait(false);

            if (result is null)
            {
                return codeAction;
            }

            return result;
        }
        finally
        {
            codeAction.Data = originalData;
        }
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostCodeActionsResolveEndpoint instance)
    {
        public Task<CodeAction?> HandleRequestAsync(TextDocument razorDocument, CodeAction request, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);

        public static Task<CodeAction> ResolveCodeActionAsync(Document document, CodeAction codeAction, ResourceOperationKind[] resourceOperations, CancellationToken cancellationToken)
            => CohostCodeActionsResolveEndpoint.ResolveCodeActionAsync(document, codeAction, resourceOperations, cancellationToken);
    }
}
