// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
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

    public ValueTask<CodeActionRequestInfo> GetCodeActionRequestInfoAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, VSCodeActionParams request, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => GetCodeActionRequestInfoAsync(snapshot, request, cancellationToken),
            cancellationToken);

    private async ValueTask<CodeActionRequestInfo> GetCodeActionRequestInfoAsync(RemoteDocumentSnapshot snapshot, VSCodeActionParams request, CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var absoluteIndex = codeDocument.Source.Text.GetRequiredAbsoluteIndex(request.Range.Start);
        var positionInfo = GetPositionInfo(codeDocument, absoluteIndex);

        VSCodeActionParams? csharpRequest = null;
        VSCodeActionParams? csharpDeclRequest = null;
        if (positionInfo.LanguageKind == RazorLanguageKind.CSharp)
        {
            // Some Razor positions can map to both C# documents. For example, @using directives and the generated class declaration
            // can exist in both impl and decl documents. Diagnostics can be reported on whichever C# document the Roslyn compiler
            // happened to see first, so ask both documents for code actions.
            await SetCSharpRequestAsync(positionInfo.InDeclDocument).ConfigureAwait(false);

            // Impl documents always exist, but decl documents are optional. If the other C# document exists, ask Roslyn for code actions there too.
            if (codeDocument.GetCSharpDocument(!positionInfo.InDeclDocument) is not null)
            {
                await SetCSharpRequestAsync(!positionInfo.InDeclDocument).ConfigureAwait(false);
            }
        }

        return new CodeActionRequestInfo(positionInfo.LanguageKind, csharpRequest, csharpDeclRequest);

        async Task SetCSharpRequestAsync(bool inDeclDocument)
        {
            var generatedRequest = await _codeActionsService.GetCSharpCodeActionsRequestAsync(snapshot, request, inDeclDocument, cancellationToken).ConfigureAwait(false);

            if (generatedRequest is null)
            {
                return;
            }

            // Since we're here, we may as well fill in the generated document Uri so the other caller won't have to calculate it
            var generatedDocument = await snapshot.GetGeneratedDocumentAsync(inDeclDocument, cancellationToken).ConfigureAwait(false);
            generatedRequest.TextDocument.DocumentUri = generatedDocument.GetURI();

            if (inDeclDocument)
            {
                csharpDeclRequest = generatedRequest;
            }
            else
            {
                csharpRequest = generatedRequest;
            }
        }
    }

    public ValueTask<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, VSCodeActionParams request, RazorVSInternalCodeAction[] htmlCodeActions, RazorVSInternalCodeAction[] csharpCodeActions, RazorVSInternalCodeAction[] csharpDeclCodeActions, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => GetCodeActionsAsync(snapshot, request, htmlCodeActions, csharpCodeActions, csharpDeclCodeActions, cancellationToken),
            cancellationToken);

    private async ValueTask<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(RemoteDocumentSnapshot snapshot, VSCodeActionParams request, RazorVSInternalCodeAction[] htmlCodeActions, RazorVSInternalCodeAction[] csharpCodeActions, RazorVSInternalCodeAction[] csharpDeclCodeActions, CancellationToken cancellationToken)
    {
        var supportsCodeActionResolve = _clientCapabilitiesService.ClientCapabilities.TextDocument?.CodeAction?.ResolveSupport is not null;
        return await _codeActionsService.GetCodeActionsAsync(request, snapshot, htmlCodeActions, csharpCodeActions, csharpDeclCodeActions, supportsCodeActionResolve, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<CodeAction> ResolveCodeActionAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, CodeAction request, CodeAction? delegatedCodeAction, CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            snapshot => ResolveCodeActionAsync(snapshot, request, delegatedCodeAction, cancellationToken),
            cancellationToken);

    private ValueTask<CodeAction> ResolveCodeActionAsync(RemoteDocumentSnapshot snapshot, CodeAction request, CodeAction? delegatedCodeAction, CancellationToken cancellationToken)
    {
        return new(_codeActionResolveService.ResolveCodeActionAsync(snapshot, request, delegatedCodeAction, cancellationToken));
    }
}
