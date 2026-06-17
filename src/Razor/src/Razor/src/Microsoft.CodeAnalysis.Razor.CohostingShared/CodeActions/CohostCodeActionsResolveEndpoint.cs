// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
#if !VSCODE
// Visual Studio requires us to register for every method name, VS Code correctly realises that if you
// register for code actions, and say you have resolve support, then registering for resolve is unnecessary.
// In fact it's an error.
[Export(typeof(IDynamicRegistrationProvider))]
#endif
[Shared]
[CohostEndpoint(Methods.CodeActionResolveName)]
[ExportRazorStatelessLspService(typeof(CohostCodeActionsResolveEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostCodeActionsResolveEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IClientCapabilitiesService clientCapabilitiesService,
    IHtmlRequestInvoker requestInvoker)
    : AbstractCohostDocumentEndpoint<CodeAction, CodeAction?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.CodeAction?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = Methods.CodeActionResolveName,
                RegisterOptions = new CodeActionRegistrationOptions().EnableCodeActions()
            }];
        }

        return [];
    }

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(CodeAction request)
    {
        var resolveParams = RazorCodeActionResolutionParams.Unwrap(request);
        return resolveParams.TextDocument;
    }

    protected override async Task<CodeAction?> HandleRequestAsync(CodeAction request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var resolveParams = RazorCodeActionResolutionParams.Unwrap(request);

        var resolvedDelegatedCodeAction = resolveParams.Language switch
        {
            RazorLanguageKind.Html => await ResolvedHtmlCodeActionAsync(razorDocument, request, resolveParams, cancellationToken).ConfigureAwait(false),
            RazorLanguageKind.CSharp => await ResolveCSharpCodeActionAsync(razorDocument, request, resolveParams, cancellationToken).ConfigureAwait(false),
            _ => null
        };

        return await _remoteServiceInvoker.TryInvokeAsync<IRemoteCodeActionsService, CodeAction>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.ResolveCodeActionAsync(solutionInfo, razorDocument.Id, request, resolvedDelegatedCodeAction, cancellationToken),
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
