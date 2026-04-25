// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed partial class RemoteCodeActionsService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteCodeActionsService
{
    internal sealed class Factory : FactoryBase<IRemoteCodeActionsService>
    {
        protected override IRemoteCodeActionsService CreateService(in ServiceArgs args)
            => new RemoteCodeActionsService(in args);
    }

    private readonly ICodeActionsService _codeActionsService = args.ExportProvider.GetExportedValue<ICodeActionsService>();
    private readonly ICodeActionResolveService _codeActionResolveService = args.ExportProvider.GetExportedValue<ICodeActionResolveService>();
    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();

    public ValueTask<CodeActionRequestInfo> GetCodeActionRequestInfoAsync(JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, VSCodeActionParams request, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetCodeActionRequestInfoAsync(context, request, cancellationToken),
            cancellationToken);

    private async ValueTask<CodeActionRequestInfo> GetCodeActionRequestInfoAsync(RemoteDocumentContext context, VSCodeActionParams request, CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var absoluteIndex = codeDocument.Source.Text.GetRequiredAbsoluteIndex(request.Range.Start);
        var languageKind = GetPositionInfo(codeDocument, absoluteIndex).LanguageKind;

        VSCodeActionParams? csharpRequest = null;
        if (languageKind == RazorLanguageKind.CSharp)
        {
            csharpRequest = await _codeActionsService.GetCSharpCodeActionsRequestAsync(context.Snapshot, request, cancellationToken).ConfigureAwait(false);

            if (csharpRequest is not null)
            {
                // Since we're here, we may as well fill in the generated document Uri so the other caller won't have to calculate it
                var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);
                csharpRequest.TextDocument.DocumentUri = generatedDocument.CreateDocumentUri();
            }
        }

        return new CodeActionRequestInfo(languageKind, csharpRequest);
    }

    public ValueTask<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, VSCodeActionParams request, RazorVSInternalCodeAction[] delegatedCodeActions, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetCodeActionsAsync(context, request, delegatedCodeActions, cancellationToken),
            cancellationToken);

    private async ValueTask<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(RemoteDocumentContext context, VSCodeActionParams request, RazorVSInternalCodeAction[] delegatedCodeActions, CancellationToken cancellationToken)
    {
        var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);
        var generatedDocumentUri = generatedDocument.CreateUri();

        var supportsCodeActionResolve = _clientCapabilitiesService.ClientCapabilities.TextDocument?.CodeAction?.ResolveSupport is not null;
        return await _codeActionsService.GetCodeActionsAsync(request, context.Snapshot, delegatedCodeActions, generatedDocumentUri, supportsCodeActionResolve, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<CodeAction> ResolveCodeActionAsync(JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, CodeAction request, CodeAction? delegatedCodeAction, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => ResolveCodeActionAsync(context, request, delegatedCodeAction, cancellationToken),
            cancellationToken);

    private ValueTask<CodeAction> ResolveCodeActionAsync(RemoteDocumentContext context, CodeAction request, CodeAction? delegatedCodeAction, CancellationToken cancellationToken)
    {
        return new(_codeActionResolveService.ResolveCodeActionAsync(context, request, delegatedCodeAction, cancellationToken));
    }
}
