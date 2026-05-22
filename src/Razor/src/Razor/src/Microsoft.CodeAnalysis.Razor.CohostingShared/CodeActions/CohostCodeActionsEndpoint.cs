// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Telemetry;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentCodeActionName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(CohostCodeActionsEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostCodeActionsEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IClientCapabilitiesService clientCapabilitiesService,
    IHtmlRequestInvoker requestInvoker,
    ITelemetryReporter telemetryReporter)
    : AbstractCohostDocumentEndpoint<VSCodeActionParams, SumType<Command, CodeAction>[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.CodeAction?.DynamicRegistration == true)
        {
            return [new Registration
            {
                Method = Methods.TextDocumentCodeActionName,
                RegisterOptions = new CodeActionRegistrationOptions().EnableCodeActions()
            }];
        }

        return [];
    }

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(VSCodeActionParams request)
        => request.TextDocument;

    protected override async Task<SumType<Command, CodeAction>[]?> HandleRequestAsync(VSCodeActionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid();
        using var _ = _telemetryReporter.TrackLspRequest(Methods.TextDocumentCodeActionName, LanguageServerConstants.RazorLanguageServerName, TelemetryThresholds.CodeActionRazorTelemetryThreshold, correlationId);

        AdjustRequestRangeIfNecessary(request);

        var requestInfo = await _remoteServiceInvoker.TryInvokeAsync<IRemoteCodeActionsService, CodeActionRequestInfo>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetCodeActionRequestInfoAsync(solutionInfo, razorDocument.Id, request, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (requestInfo is null or { LanguageKind: RazorLanguageKind.CSharp, CSharpRequest: null })
        {
            return null;
        }

        // This is just to prevent a warning for an unused field in the VS Code extension
        Debug.Assert(_requestInvoker is not null);

        var delegatedCodeActions = requestInfo.LanguageKind switch
        {
            // We don't support Html code actions in VS Code
#if !VSCODE
            RazorLanguageKind.Html => await GetHtmlCodeActionsAsync(razorDocument, request, correlationId, cancellationToken).ConfigureAwait(false),
#endif
            RazorLanguageKind.CSharp => await GetCSharpCodeActionsAsync(razorDocument, requestInfo.CSharpRequest.AssumeNotNull(), correlationId, cancellationToken).ConfigureAwait(false),
            _ => []
        };

        return await _remoteServiceInvoker.TryInvokeAsync<IRemoteCodeActionsService, SumType<Command, CodeAction>[]?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetCodeActionsAsync(solutionInfo, razorDocument.Id, request, delegatedCodeActions, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<RazorVSInternalCodeAction[]> GetCSharpCodeActionsAsync(TextDocument razorDocument, VSCodeActionParams request, Guid correlationId, CancellationToken cancellationToken)
    {
        var generatedDocument = await razorDocument.Project.Solution.TryGetSourceGeneratedDocumentAsync(request.TextDocument.DocumentUri.GetRequiredSystemUri(), cancellationToken).ConfigureAwait(false);
        if (generatedDocument is null)
        {
            return [];
        }

        // We have to use our own type, which doesn't inherit from CodeActionParams, so we have to use Json to convert
        var csharpRequest = JsonHelpers.Convert<VSCodeActionParams, CodeActionParams>(request).AssumeNotNull();

        using var _ = _telemetryReporter.TrackLspRequest(Methods.TextDocumentCodeActionName, "Razor.ExternalAccess", TelemetryThresholds.CodeActionSubLSPTelemetryThreshold, correlationId);
        var csharpCodeActions = await CodeActions.GetCodeActionsAsync(generatedDocument, csharpRequest, _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions, cancellationToken).ConfigureAwait(false);

        return JsonHelpers.ConvertAll<CodeAction, RazorVSInternalCodeAction>(csharpCodeActions);
    }

#if !VSCODE
    private async Task<RazorVSInternalCodeAction[]> GetHtmlCodeActionsAsync(TextDocument razorDocument, VSCodeActionParams request, Guid correlationId, CancellationToken cancellationToken)
    {
        var result = await _requestInvoker.MakeHtmlLspRequestAsync<VSCodeActionParams, RazorVSInternalCodeAction[]>(
            razorDocument,
            Methods.TextDocumentCodeActionName,
            request,
            TelemetryThresholds.CodeActionSubLSPTelemetryThreshold,
            correlationId,
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return [];
        }

        return result;
    }
#endif

    private static void AdjustRequestRangeIfNecessary(VSCodeActionParams request)
    {
        // VS Provides `CodeActionParams.Context.SelectionRange` in addition to
        // `CodeActionParams.Range`. The `SelectionRange` is relative to where the
        // code action was invoked (ex. line 14, char 3) whereas the `Range` is
        // always at the start of the line (ex. line 14, char 0). We want to utilize
        // the relative positioning to ensure we provide code actions for the appropriate
        // context.
        //
        // We only do this if the Range contains the SelectionRange, or in other words if
        // the SelectionRange serves to better focus the Range. It is possible for the selection
        // to be on one line, and the code action request to be for an entirely different line
        // if the user is invoking from the lightbulb button directly, for example on hovering
        // over a diagnostic. In those cases, using SelectionRange would be wrong.
        //
        // Note: VS Code doesn't provide a `SelectionRange`.
        if (request.Context.SelectionRange is { } selectionRange &&
            request.Range.Contains(selectionRange))
        {
            request.Range = selectionRange;
        }
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostCodeActionsEndpoint instance)
    {
        public Task<SumType<Command, CodeAction>[]?> HandleRequestAsync(TextDocument razorDocument, VSCodeActionParams request, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
