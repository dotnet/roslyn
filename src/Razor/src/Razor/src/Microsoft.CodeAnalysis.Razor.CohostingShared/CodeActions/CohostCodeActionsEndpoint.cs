// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
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
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Logging;
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
    ITelemetryReporter telemetryReporter,
    ILoggerFactory loggerFactory)
    : AbstractCohostDocumentEndpoint<VSCodeActionParams, SumType<Command, CodeAction>[]?>(incompatibleProjectService), IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostCodeActionsEndpoint>();

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
        LogCodeActionTrace($"Cohost.Request: RazorDocument='{FormatDocument(razorDocument)}', Range='{FormatRange(request.Range)}', SelectionRange='{FormatRange(request.Context.SelectionRange)}', Diagnostics=[{FormatDiagnostics(request.Context.Diagnostics)}]");

        var requestInfo = await _remoteServiceInvoker.TryInvokeAsync<IRemoteCodeActionsService, CodeActionRequestInfo>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetCodeActionRequestInfoAsync(solutionInfo, razorDocument.Id, request, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (requestInfo is null or { LanguageKind: RazorLanguageKind.CSharp, CSharpRequest: null })
        {
            LogCodeActionTrace($"Cohost.RequestInfoMissing: RequestInfo='{requestInfo}', RazorDocument='{FormatDocument(razorDocument)}'");
            return null;
        }

        LogCodeActionTrace($"Cohost.RequestInfo: LanguageKind='{requestInfo.LanguageKind}', HasCSharpRequest={requestInfo.CSharpRequest is not null}, CSharpRange='{FormatRange(requestInfo.CSharpRequest?.Range)}', CSharpDiagnostics=[{FormatDiagnostics(requestInfo.CSharpRequest?.Context.Diagnostics)}]");

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

        LogCodeActionTrace($"Cohost.DelegatedActions: LanguageKind='{requestInfo.LanguageKind}', Count={delegatedCodeActions.Length}, Actions=[{FormatRazorCodeActions(delegatedCodeActions)}]");

        var result = await _remoteServiceInvoker.TryInvokeAsync<IRemoteCodeActionsService, SumType<Command, CodeAction>[]?>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetCodeActionsAsync(solutionInfo, razorDocument.Id, request, delegatedCodeActions, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        LogCodeActionTrace($"Cohost.FinalResult: Count={result?.Length.ToString() ?? "<null>"}, Actions=[{FormatSumTypeCodeActions(result)}]");
        return result;
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
        LogCodeActionTrace($"Cohost.CSharpRequest: RazorDocument='{FormatDocument(razorDocument)}', GeneratedDocument='{FormatDocument(generatedDocument)}', Range='{FormatRange(request.Range)}', Diagnostics=[{FormatDiagnostics(request.Context.Diagnostics)}]");
        var csharpCodeActions = await GetCodeActionsAsync(generatedDocument, csharpRequest, _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions, cancellationToken).ConfigureAwait(false);
        LogCodeActionTrace($"Cohost.CSharpRawActions: Count={csharpCodeActions.Length}, Actions=[{FormatCodeActions(csharpCodeActions)}]");

        var convertedCodeActions = JsonHelpers.ConvertAll<CodeAction, RazorVSInternalCodeAction>(csharpCodeActions);
        LogCodeActionTrace($"Cohost.CSharpConvertedActions: Count={convertedCodeActions.Length}, Actions=[{FormatRazorCodeActions(convertedCodeActions)}]");
        return convertedCodeActions;
    }

    private static Task<CodeAction[]> GetCodeActionsAsync(
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

        public static Task<CodeAction[]> GetCodeActionsAsync(Document document, CodeActionParams request, bool supportsVSExtensions, CancellationToken cancellationToken)
            => CohostCodeActionsEndpoint.GetCodeActionsAsync(document, request, supportsVSExtensions, cancellationToken);
    }

    private void LogCodeActionTrace(string message)
    {
        var fullMessage = $"RazorCodeActionTrace {message}";
        _logger.LogDebug(fullMessage);
        Trace.WriteLine(fullMessage);
    }

    private static string FormatCodeActions(CodeAction[]? codeActions)
    {
        if (codeActions is null)
        {
            return "<null>";
        }

        if (codeActions.Length == 0)
        {
            return "<empty>";
        }

        return string.Join("; ", codeActions.Select(static (action, index) =>
            $"{index}: Title='{action.Title}', Kind='{action.Kind?.ToString() ?? "<null>"}', Data={FormatData(action.Data)}"));
    }

    private static string FormatRazorCodeActions(RazorVSInternalCodeAction[]? codeActions)
    {
        if (codeActions is null)
        {
            return "<null>";
        }

        if (codeActions.Length == 0)
        {
            return "<empty>";
        }

        return string.Join("; ", codeActions.Select(static (action, index) =>
            $"{index}: Name='{action.Name ?? "<null>"}', Title='{action.Title}', Kind='{action.Kind?.ToString() ?? "<null>"}', Group='{action.Group ?? "<null>"}', Children={action.Children?.Length ?? 0}, Command='{action.Command?.CommandIdentifier ?? "<null>"}', Data={FormatData(action.Data)}"));
    }

    private static string FormatSumTypeCodeActions(SumType<Command, CodeAction>[]? codeActions)
    {
        if (codeActions is null)
        {
            return "<null>";
        }

        if (codeActions.Length == 0)
        {
            return "<empty>";
        }

        return string.Join("; ", codeActions.Select(static (action, index) =>
            $"{index}: {FormatCodeActionValue(action.Value)}"));
    }

    private static string FormatCodeActionValue(object? value)
        => value switch
        {
            RazorVSInternalCodeAction razorAction => $"RazorVSInternalCodeAction Name='{razorAction.Name ?? "<null>"}' Title='{razorAction.Title}' Kind='{razorAction.Kind?.ToString() ?? "<null>"}' Data={FormatData(razorAction.Data)}",
            VSInternalCodeAction vsAction => $"VSInternalCodeAction Title='{vsAction.Title}' Kind='{vsAction.Kind?.ToString() ?? "<null>"}' Data={FormatData(vsAction.Data)}",
            CodeAction action => $"CodeAction Title='{action.Title}' Kind='{action.Kind?.ToString() ?? "<null>"}' Data={FormatData(action.Data)}",
            Command command => $"Command Title='{command.Title}' Command='{command.CommandIdentifier}'",
            null => "<null>",
            _ => $"{value.GetType().FullName}: {value}"
        };

    private static string FormatData(object? data)
    {
        if (data is null)
        {
            return "<null>";
        }

        if (data is not JsonElement jsonData)
        {
            return data.GetType().FullName ?? data.GetType().Name;
        }

        var customTags = jsonData.TryGetProperty("CustomTags", out var customTagsValue) && customTagsValue.ValueKind == JsonValueKind.Array
            ? $"CustomTags=[{string.Join(", ", customTagsValue.EnumerateArray().Select(static tag => tag.GetString()))}]"
            : "CustomTags=<missing>";

        var fixAllFlavors = jsonData.TryGetProperty("FixAllFlavors", out var fixAllFlavorsValue) && fixAllFlavorsValue.ValueKind == JsonValueKind.Array
            ? $"FixAllFlavors={fixAllFlavorsValue.GetArrayLength()}"
            : "FixAllFlavors=<missing>";

        var uniqueIdentifier = jsonData.TryGetProperty("UniqueIdentifier", out var uniqueIdentifierValue)
            ? $"UniqueIdentifier='{uniqueIdentifierValue}'"
            : "UniqueIdentifier=<missing>";

        return $"JsonElement{{ValueKind={jsonData.ValueKind}, {customTags}, {fixAllFlavors}, {uniqueIdentifier}}}";
    }

    private static string FormatDiagnostics(IEnumerable<LspDiagnostic>? diagnostics)
    {
        if (diagnostics is null)
        {
            return "<null>";
        }

        return string.Join("; ", diagnostics.Select(static (diagnostic, index) =>
            $"{index}: Code='{diagnostic.Code}', Severity='{diagnostic.Severity}', Range='{FormatRange(diagnostic.Range)}', Message='{diagnostic.Message}'"));
    }

    private static string FormatRange(LspRange? range)
        => range is null
            ? "<null>"
            : $"{range.Start.Line}:{range.Start.Character}-{range.End.Line}:{range.End.Character}";

    private static string FormatDocument(TextDocument document)
        => document.FilePath ?? document.Name;
}
