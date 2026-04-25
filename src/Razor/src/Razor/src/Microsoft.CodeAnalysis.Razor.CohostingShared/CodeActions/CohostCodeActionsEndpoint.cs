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
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions;
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

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
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

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(VSCodeActionParams request)
        => request.TextDocument.ToRazorTextDocumentIdentifier();

    protected override async Task<SumType<Command, CodeAction>[]?> HandleRequestAsync(VSCodeActionParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid();
        using var _ = _telemetryReporter.TrackLspRequest(Methods.TextDocumentCodeActionName, LanguageServerConstants.RazorLanguageServerName, TelemetryThresholds.CodeActionRazorTelemetryThreshold, correlationId);

        CodeActionsService.AdjustRequestRangeIfNecessary(request);

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
        var generatedDocument = await razorDocument.Project.Solution.TryGetSourceGeneratedDocumentAsync(request.TextDocument.DocumentUri.GetRequiredParsedUri(), cancellationToken).ConfigureAwait(false);
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

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostCodeActionsEndpoint instance)
    {
        public Task<SumType<Command, CodeAction>[]?> HandleRequestAsync(TextDocument razorDocument, VSCodeActionParams request, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
