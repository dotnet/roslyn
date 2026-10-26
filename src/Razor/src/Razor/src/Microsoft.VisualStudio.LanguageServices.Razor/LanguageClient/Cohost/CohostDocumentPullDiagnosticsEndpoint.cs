// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
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
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(PublicCohostDocumentPullDiagnosticsEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class PublicCohostDocumentPullDiagnosticsEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker,
    IClientCapabilitiesService clientCapabilitiesService,
    ITelemetryReporter telemetryReporter,
    ILoggerFactory loggerFactory,
    IEditAndContinueSessionTracker encSessionTracker)
    : CohostDocumentPullDiagnosticsEndpointBase<
        DocumentDiagnosticParams,
        FullDocumentDiagnosticReport?,
        SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>>(
        incompatibleProjectService,
        remoteServiceInvoker,
        requestInvoker,
        clientCapabilitiesService,
        telemetryReporter,
        loggerFactory.GetOrCreateLogger<PublicCohostDocumentPullDiagnosticsEndpoint>(),
        encSessionTracker),
      IDynamicRegistrationProvider
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IClientCapabilitiesService _clientCapabilitiesService = clientCapabilitiesService;

    protected override string LspMethodName => Methods.TextDocumentDiagnosticName;
    protected override bool SupportsHtmlDiagnostics => true;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.Diagnostic?.DynamicRegistration is true)
        {
            return [new Registration()
            {
                Method = Methods.TextDocumentDiagnosticName,
                RegisterOptions = new DiagnosticRegistrationOptions()
                {
                    Identifier = PullDiagnosticCategories.DocumentCompilerSyntax,
                }
            },
            new Registration()
            {
                Method = Methods.TextDocumentDiagnosticName,
                RegisterOptions = new DiagnosticRegistrationOptions()
                {
                    Identifier = PullDiagnosticCategories.Task,
                }
            }];
        }

        return [];
    }

    protected override TextDocumentIdentifier? GetRazorTextDocumentIdentifier(DocumentDiagnosticParams request)
        => request.TextDocument;

    protected override async Task<FullDocumentDiagnosticReport?> HandleRequestAsync(DocumentDiagnosticParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        if (request.Identifier == PullDiagnosticCategories.Task)
        {
            var taskListDiagnostics = await GetTaskListDiagnosticsAsync(razorDocument, cancellationToken).ConfigureAwait(false);
            return new()
            {
                Items = taskListDiagnostics,
                ResultId = taskListDiagnostics.Length == 0 ? null : Guid.NewGuid().ToString()
            };
        }

        var results = await GetVSDiagnosticsAsync(request, razorDocument, cancellationToken).ConfigureAwait(false);
        if (results is null)
        {
            return null;
        }

        return new()
        {
            Items = results,
            ResultId = Guid.NewGuid().ToString()
        };
    }

    private static DocumentDiagnosticParams CreateHtmlParams(DocumentDiagnosticParams request, DocumentUri uri)
    {
        return new DocumentDiagnosticParams
        {
            TextDocument = new TextDocumentIdentifier { DocumentUri = uri },
            Identifier = request.Identifier,
        };
    }

    protected override LspDiagnostic[] ExtractHtmlDiagnostics(SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport> result)
        => result.Value is FullDocumentDiagnosticReport report ? report.Items : [];

    private async Task<LspDiagnostic[]?> GetVSDiagnosticsAsync(DocumentDiagnosticParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var diagnostics = await GetDiagnosticsAsync(razorDocument, cancellationToken, uri => CreateHtmlParams(request, uri)).ConfigureAwait(false);
        if (diagnostics is null)
        {
            return null;
        }

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

    internal readonly struct TestAccessor(PublicCohostDocumentPullDiagnosticsEndpoint instance)
    {
        public Task<FullDocumentDiagnosticReport?> HandleRequestAsync(DocumentDiagnosticParams request, TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.HandleRequestAsync(request, razorDocument, cancellationToken);
    }
}

