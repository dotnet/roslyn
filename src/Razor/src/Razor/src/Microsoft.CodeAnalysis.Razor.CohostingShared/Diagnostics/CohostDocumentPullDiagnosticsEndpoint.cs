// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.CohostingShared;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Telemetry;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentDiagnosticName)]
[ExportRazorStatelessLspService(typeof(CohostDocumentPullDiagnosticsEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class CohostDocumentPullDiagnosticsEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker,
    IClientCapabilitiesService clientCapabilitiesService,
    ITelemetryReporter telemetryReporter,
    ILoggerFactory loggerFactory,
    IEditAndContinueSessionTracker encSessionTracker)
    : AbstractCohostDocumentEndpoint<DocumentDiagnosticParams, FullDocumentDiagnosticReport?>(incompatibleProjectService)
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlRequestInvoker _requestInvoker = requestInvoker;
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<CohostDocumentPullDiagnosticsEndpoint>();
    private readonly IEditAndContinueSessionTracker _encSessionTracker = encSessionTracker;

    protected override bool MutatesSolutionState => false;

    protected override bool RequiresLSPSolution => true;

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(DocumentDiagnosticParams request)
        => request.TextDocument;

    protected override async Task<FullDocumentDiagnosticReport?> HandleRequestAsync(DocumentDiagnosticParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var supportsVisualStudioExtensions = _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions;
        if (supportsVisualStudioExtensions && request.Identifier == PullDiagnosticCategories.Task)
        {
            var taskListDiagnostics = await GetTaskListDiagnosticsAsync(razorDocument, cancellationToken).ConfigureAwait(false);
            return new()
            {
                Items = taskListDiagnostics,
                ResultId = taskListDiagnostics.Length == 0 ? null : Guid.NewGuid().ToString()
            };
        }

        var results = await GetDiagnosticsAsync(request, razorDocument, cancellationToken).ConfigureAwait(false);
        if (results is null)
        {
            return null;
        }

        if (supportsVisualStudioExtensions)
        {
            results = PopulateVSDiagnosticMetadata(results, razorDocument);
        }

        return new()
        {
            Items = results,
            ResultId = Guid.NewGuid().ToString()
        };
    }

    private async Task<LspDiagnostic[]?> GetDiagnosticsAsync(DocumentDiagnosticParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid();
        using var _ = _telemetryReporter.TrackLspRequest(Methods.TextDocumentDiagnosticName, LanguageServerConstants.RazorLanguageServerName, TelemetryThresholds.DiagnosticsRazorTelemetryThreshold, correlationId);

        // Diagnostics is a little different, because Roslyn is not designed to run diagnostics in OOP. Their system will transition to OOP
        // as it needs, but we have to start in the host process. This is not as big a problem as it sounds, specifically for diagnostics, because
        // we only need to tell Roslyn the document we need diagnostics for. If we had to map positions or ranges etc. it would be worse
        // because we'd have to transition to our OOP to find out that info, then back here to get the diagnostics, then back to OOP to process.
        _logger.LogDebug($"Getting diagnostics for {razorDocument.FilePath}");

        var csharpTask = GetCSharpDiagnosticsAsync(razorDocument, correlationId, cancellationToken);

        // We only report HTML diagnostics in VS.
        var htmlTask = _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions
            ? GetHtmlDiagnosticsAsync(request, razorDocument, correlationId, cancellationToken)
            : SpecializedTasks.EmptyArray<LspDiagnostic>();

        try
        {
            await Task.WhenAll(htmlTask, csharpTask).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, $"Exception thrown in PullDiagnostic delegation");
            throw;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        var (implDiagnostics, declDiagnostics) = csharpTask.VerifyCompleted();
        var htmlDiagnostics = htmlTask.VerifyCompleted();

        _logger.LogDebug($"Calling OOP with the {implDiagnostics.Length} impl C# and {declDiagnostics.Length} decl C# and {htmlDiagnostics.Length} Html diagnostics");
        var diagnostics = await _remoteServiceInvoker.TryInvokeAsync<IRemoteDiagnosticsService, ImmutableArray<LspDiagnostic>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetDiagnosticsAsync(solutionInfo, razorDocument.Id, implDiagnostics, declDiagnostics, htmlDiagnostics, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested || diagnostics.IsDefault)
        {
            return null;
        }

        _logger.LogDebug($"Reporting {diagnostics.Length} diagnostics back to the client");
        return [.. diagnostics];
    }

    private async Task<(LspDiagnostic[], LspDiagnostic[])> GetCSharpDiagnosticsAsync(TextDocument razorDocument, Guid correlationId, CancellationToken cancellationToken)
    {
        // Because we can't map from a random C# point to a Razor point without knowing which C# document we're talking about, we have to just make two requests
        // and send back two sets of diagnostics

        var csharpDocs = await razorDocument.Project.TryGetSourceGeneratedDocumentsForRazorDocumentAsync(razorDocument, cancellationToken).ConfigureAwait(false);
        if (csharpDocs is not { } generatedDocuments)
        {
            return ([], []);
        }

        using var _ = _telemetryReporter.TrackLspRequest(Methods.TextDocumentDiagnosticName, "Razor.ExternalAccess", TelemetryThresholds.DiagnosticsSubLSPTelemetryThreshold, correlationId);
        var supportsVisualStudioExtensions = _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions;

        _logger.LogDebug($"Getting C# diagnostics for {generatedDocuments.ImplDoc.FilePath}");
        var implDiagnostics = await CohostDocumentPullDiagnosticsHelpers.GetDocumentDiagnosticsAsync(generatedDocuments.ImplDoc, _encSessionTracker, supportsVisualStudioExtensions, cancellationToken).ConfigureAwait(false);

        if (generatedDocuments.DeclDoc is null)
        {
            return (implDiagnostics, []);
        }

        _logger.LogDebug($"Getting C# diagnostics for {generatedDocuments.DeclDoc.FilePath}");
        var declDiagnostics = await CohostDocumentPullDiagnosticsHelpers.GetDocumentDiagnosticsAsync(generatedDocuments.DeclDoc, _encSessionTracker, supportsVisualStudioExtensions, cancellationToken).ConfigureAwait(false);

        return (implDiagnostics, declDiagnostics);
    }

    private async Task<LspDiagnostic[]> GetHtmlDiagnosticsAsync(DocumentDiagnosticParams request, TextDocument razorDocument, Guid correlationId, CancellationToken cancellationToken)
    {
        var diagnosticsParams = new DocumentDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = razorDocument.GetURI() },
            Identifier = request.Identifier,
        };

        var result = await _requestInvoker.MakeHtmlLspRequestAsync<DocumentDiagnosticParams, SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>>(
            razorDocument,
            Methods.TextDocumentDiagnosticName,
            diagnosticsParams,
            TelemetryThresholds.DiagnosticsSubLSPTelemetryThreshold,
            correlationId,
            cancellationToken).ConfigureAwait(false);

        return result.Value is FullDocumentDiagnosticReport report ? report.Items : [];
    }

    private static LspDiagnostic[] PopulateVSDiagnosticMetadata(LspDiagnostic[] diagnostics, TextDocument razorDocument)
    {
        // We always use Roslyn's project understanding, and in VS the project Id is not necessarily the Id that is reported by Roslyn
        // for diagnostics. Rather than try to replicate any of this behaviour directly, we just take Roslyn as the source of truth,
        // and force the project information to match what it would produce, regardless of where it comes from or how we might have
        // filtered or converted it.
        var service = razorDocument.Project.Solution.Services.GetRequiredService<IDiagnosticProjectInformationService>();
        var projectInfo = new[] { service.GetDiagnosticProjectInformation(razorDocument.Project) };

        var results = new VSDiagnostic[diagnostics.Length];
        for (var i = 0; i < diagnostics.Length; i++)
        {
            var vsDiagnostic = JsonHelpers.Convert<LspDiagnostic, VSDiagnostic>(diagnostics[i]).AssumeNotNull();
            vsDiagnostic.Projects = projectInfo;

            // Setting a unique identifier ensures that VS will show project info in the error list, and things like the "Current Project"
            // filter will work. Putting the Razor file path in the identifier ensures that files in multiple projects get their diagnostics
            // de-duped.
            vsDiagnostic.Identifier = (vsDiagnostic.Code, razorDocument.FilePath, vsDiagnostic.Range, vsDiagnostic.Message).GetHashCode().ToString();

            results[i] = vsDiagnostic;
        }

        return results;
    }

    private async Task<LspDiagnostic[]> GetTaskListDiagnosticsAsync(TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var (implTaskItems, declTaskItems) = await GetCSharpTaskListItemsAsync(razorDocument, cancellationToken).ConfigureAwait(false);

        var diagnostics = await _remoteServiceInvoker.TryInvokeAsync<IRemoteDiagnosticsService, ImmutableArray<LspDiagnostic>>(
            razorDocument.Project.Solution,
            (service, solutionInfo, cancellationToken) => service.GetTaskListDiagnosticsAsync(solutionInfo, razorDocument.Id, implTaskItems, declTaskItems, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return diagnostics.IsDefaultOrEmpty ? [] : [.. diagnostics];
    }

    private async Task<(LspDiagnostic[], LspDiagnostic[])> GetCSharpTaskListItemsAsync(TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var csharpDocs = await razorDocument.Project.TryGetSourceGeneratedDocumentsForRazorDocumentAsync(razorDocument, cancellationToken).ConfigureAwait(false);
        if (csharpDocs is not { } generatedDocuments)
        {
            return ([], []);
        }

        var supportsVisualStudioExtensions = _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions;
        var solutionServices = generatedDocuments.ImplDoc.Project.Solution.Services;
        var globalOptionsService = solutionServices.ExportProvider.GetService<IGlobalOptionService>();

        var implItems = await GetTaskListItemsAsync(generatedDocuments.ImplDoc).ConfigureAwait(false);
        var declItems = await GetTaskListItemsAsync(generatedDocuments.DeclDoc).ConfigureAwait(false);

        return (implItems, declItems);

        async Task<LspDiagnostic[]> GetTaskListItemsAsync(SourceGeneratedDocument? doc)
        {
            if (doc is null)
            {
                return [];
            }

            var implItems = await TaskListDiagnosticSource.GetTaskListItemsAsync(doc, globalOptionsService, cancellationToken).ConfigureAwait(false);
            return CohostDocumentPullDiagnosticsHelpers.ConvertDiagnostics(doc, supportsVisualStudioExtensions, globalOptionsService, implItems);
        }
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(CohostDocumentPullDiagnosticsEndpoint instance)
    {
        public Task<FullDocumentDiagnosticReport?> HandleRequestAsync(DocumentDiagnosticParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}
