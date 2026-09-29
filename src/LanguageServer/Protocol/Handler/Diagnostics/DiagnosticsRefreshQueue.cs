// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

[ExportCSharpVisualBasicLspService(typeof(DiagnosticsRefreshQueue)), Shared(LspServiceComposition.SharingBoundary)]
internal sealed class DiagnosticsRefreshQueue : AbstractRefreshQueue
{
    private readonly IDiagnosticsRefresher _refresher;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DiagnosticsRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        FeatureProviderRefresher providerRefresher,
        IDiagnosticsRefresher refresher,
        LspService<IClientLanguageServerManager> notificationManager,
        LspService<LspWorkspaceRegistrationService> lspWorkspaceRegistrationService,
        LspService<LspWorkspaceManager> lspWorkspaceManager)
        : base(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService.Value, lspWorkspaceManager.Value, notificationManager.Value, providerRefresher)
    {
        _refresher = refresher;

        refresher.WorkspaceRefreshRequested += WorkspaceRefreshRequested;
    }

    public override void Dispose()
    {
        base.Dispose();
        _refresher.WorkspaceRefreshRequested -= WorkspaceRefreshRequested;
    }

    private void WorkspaceRefreshRequested()
        => EnqueueRefreshNotification(documentUri: null);

    protected override string GetFeatureAttribute()
        => FeatureAttribute.DiagnosticService;

    protected override bool? GetRefreshSupport(ClientCapabilities clientCapabilities)
        => clientCapabilities.Workspace?.Diagnostics?.RefreshSupport;

    protected override string GetWorkspaceRefreshName()
        => Methods.WorkspaceDiagnosticRefreshName;
}
