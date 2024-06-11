// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

[ExportCSharpVisualBasicLspServiceFactory(typeof(PublicWorkspacePullDiagnosticsHandler)), Shared]
internal sealed class PublicWorkspacePullDiagnosticHandlerFactory : ILspServiceFactory
{
    private readonly LspWorkspaceRegistrationService _registrationService;
    private readonly IDiagnosticAnalyzerService _analyzerService;
    private readonly IDiagnosticsRefresher _diagnosticsRefresher;
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PublicWorkspacePullDiagnosticHandlerFactory(
        LspWorkspaceRegistrationService registrationService,
        IDiagnosticAnalyzerService analyzerService,
        IDiagnosticsRefresher diagnosticsRefresher,
        IGlobalOptionService globalOptions)
    {
        _registrationService = registrationService;
        _analyzerService = analyzerService;
        _diagnosticsRefresher = diagnosticsRefresher;
        _globalOptions = globalOptions;
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var workspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();
        return new PublicWorkspacePullDiagnosticsHandler(workspaceManager, _registrationService, _analyzerService, _diagnosticsRefresher, _globalOptions);
    }
}
