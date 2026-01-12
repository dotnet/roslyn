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

internal sealed class DiagnosticsRefreshQueue : AbstractRefreshQueue
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(DiagnosticsRefreshQueue)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class Factory(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        IDiagnosticsRefresher refresher,
        IFeatureProviderRefresher providerRefresher) : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var notificationManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
            var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();

            return new DiagnosticsRefreshQueue(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager, providerRefresher, refresher);
        }
    }

    private readonly IDiagnosticsRefresher _refresher;

    private DiagnosticsRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        LspWorkspaceManager lspWorkspaceManager,
        IClientLanguageServerManager notificationManager,
        IFeatureProviderRefresher providerRefresher,
        IDiagnosticsRefresher refresher)
        : base(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager, providerRefresher)
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
