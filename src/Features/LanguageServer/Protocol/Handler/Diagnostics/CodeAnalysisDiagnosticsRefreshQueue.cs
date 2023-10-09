// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed partial class CodeAnalysisDiagnosticsRefreshQueue : AbstractDiagnosticsRefreshQueue
{
    // TODO: Remove the below field and instead use 'Microsoft.VisualStudio.LanguageServer.Protocol.Methods.CodeAnalysisDiagnosticRefreshName'
    // once we move to a package version of Microsoft.VisualStudio.LanguageServer.Protocol that exposes this new API.
    private const string CodeAnalysisDiagnosticRefreshName = "codeanalysis/diagnostic/refresh";

    private readonly Refresher _refresher;

    private CodeAnalysisDiagnosticsRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        LspWorkspaceManager lspWorkspaceManager,
        IClientLanguageServerManager notificationManager,
        Refresher refresher)
        : base(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager)
    {
        _refresher = refresher;

        refresher.CodeAnalysisRefreshRequested += DiagnosticsRefreshRequested;
    }

    protected override string GetWorkspaceRefreshName()
        => CodeAnalysisDiagnosticRefreshName;

    public override void Dispose()
    {
        base.Dispose();
        _refresher.CodeAnalysisRefreshRequested -= DiagnosticsRefreshRequested;
    }
}
