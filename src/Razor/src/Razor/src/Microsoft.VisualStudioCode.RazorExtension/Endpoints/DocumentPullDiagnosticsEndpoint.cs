// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Telemetry;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

#pragma warning disable RS0030 // Do not use banned APIs
[Shared]
[CohostEndpoint(Methods.TextDocumentDiagnosticName)]
[Export(typeof(IDynamicRegistrationProvider))]
[ExportRazorStatelessLspService(typeof(DocumentPullDiagnosticsEndpoint))]
[method: ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class DocumentPullDiagnosticsEndpoint(
    IIncompatibleProjectService incompatibleProjectService,
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlRequestInvoker requestInvoker,
    IClientCapabilitiesService clientCapabilitiesService,
    ITelemetryReporter telemetryReporter,
    ILoggerFactory loggerFactory)
    : CohostDocumentPullDiagnosticsEndpointBase<DocumentDiagnosticParams, FullDocumentDiagnosticReport?>(
        incompatibleProjectService,
        remoteServiceInvoker,
        requestInvoker,
        clientCapabilitiesService,
        telemetryReporter,
        loggerFactory.GetOrCreateLogger<DocumentPullDiagnosticsEndpoint>()), IDynamicRegistrationProvider
{
    protected override string LspMethodName => Methods.TextDocumentDiagnosticName;
    protected override bool SupportsHtmlDiagnostics => false;

    public ImmutableArray<Registration> GetRegistrations(VSInternalClientCapabilities clientCapabilities, RazorCohostRequestContext requestContext)
    {
        if (clientCapabilities.TextDocument?.Diagnostic?.DynamicRegistration is true)
        {
            return [new Registration()
            {
                Method = Methods.TextDocumentDiagnosticName,
                RegisterOptions = new DiagnosticRegistrationOptions()
                {
                    WorkspaceDiagnostics = false
                }
            }];
        }

        return [];
    }

    protected override RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(DocumentDiagnosticParams request)
        => request.TextDocument?.ToRazorTextDocumentIdentifier();

    protected override async Task<FullDocumentDiagnosticReport?> HandleRequestAsync(DocumentDiagnosticParams request, TextDocument razorDocument, CancellationToken cancellationToken)
    {
        var results = await GetDiagnosticsAsync(razorDocument, cancellationToken).ConfigureAwait(false);

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

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(DocumentPullDiagnosticsEndpoint instance)
    {
        public Task<LspDiagnostic[]?> HandleRequestAsync(TextDocument razorDocument, CancellationToken cancellationToken)
            => instance.GetDiagnosticsAsync(razorDocument, cancellationToken);
    }
}

