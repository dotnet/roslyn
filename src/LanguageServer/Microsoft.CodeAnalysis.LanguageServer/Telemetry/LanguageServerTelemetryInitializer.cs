// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Telemetry;

/// <summary>
/// Called by <see cref="InitializedHandler"/> to update telemetry common properties with client information.
/// </summary>
[ExportCSharpVisualBasicLspServiceFactory(typeof(TelemetryClientNameInitializeHandler), WellKnownLspServerKinds.CSharpVisualBasicLspServer), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class TelemetryClientNameInitializeHandlerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new TelemetryClientNameInitializeHandler();
}

internal sealed class TelemetryClientNameInitializeHandler : ILspService, IOnInitialize
{
    private LanguageServerTelemetry? _languageServerTelemetry;

    public Task OnInitializeAsync(InitializeParams initializeParams, RequestContext context, CancellationToken cancellationToken)
    {
        _languageServerTelemetry?.SetClientName(initializeParams.ClientInfo?.Name);
        return Task.CompletedTask;
    }

    public void SetTelemetryInstance(LanguageServerTelemetry? languageServerTelemetry)
    {
        Contract.ThrowIfFalse(_languageServerTelemetry == null);
        _languageServerTelemetry = languageServerTelemetry;
    }
}
