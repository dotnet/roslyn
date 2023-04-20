// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed class DiagnosticsRefreshQueue : AbstractRefreshQueue
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(DiagnosticsRefreshQueue)), Shared]
    internal sealed class Factory : ILspServiceFactory
    {
        private readonly IAsynchronousOperationListenerProvider _asyncListenerProvider;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
        private readonly Refresher _refresher;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Factory(
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            Refresher refresher)
        {
            _asyncListenerProvider = asynchronousOperationListenerProvider;
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _refresher = refresher;
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var notificationManager = lspServices.GetRequiredService<IClientLanguageServerManager>();
            var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();

            return new DiagnosticsRefreshQueue(_asyncListenerProvider, _lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager, _refresher);
        }
    }

    [Shared]
    [Export(typeof(Refresher))]
    [Export(typeof(IDiagnosticsRefresher))]
    internal sealed class Refresher : IDiagnosticsRefresher
    {
        public event Action? WorkspaceRefreshRequested;
        public event Action<Document>? DocumentRefreshRequested;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Refresher()
        {
        }

        public void RequestWorkspaceRefresh()
            => WorkspaceRefreshRequested?.Invoke();

        public void RequestDocumentRefresh(Document document)
            => DocumentRefreshRequested?.Invoke(document);
    }

    private readonly Refresher _refresher;

    private DiagnosticsRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        LspWorkspaceManager lspWorkspaceManager,
        IClientLanguageServerManager notificationManager,
        Refresher refresher)
        : base(asynchronousOperationListenerProvider, lspWorkspaceRegistrationService, lspWorkspaceManager, notificationManager)
    {
        _refresher = refresher;

        refresher.WorkspaceRefreshRequested += WorkspaceRefreshRequested;
        refresher.DocumentRefreshRequested += DocumentRefreshRequested;
    }

    public override void Dispose()
    {
        base.Dispose();
        _refresher.WorkspaceRefreshRequested -= WorkspaceRefreshRequested;
        _refresher.DocumentRefreshRequested -= DocumentRefreshRequested;
    }

    private void WorkspaceRefreshRequested()
        => EnqueueRefreshNotification(documentUri: null);

    private void DocumentRefreshRequested(Document document)
        => EnqueueRefreshNotification(document.GetURI());

    protected override string GetFeatureAttribute()
        => FeatureAttribute.DiagnosticService;

    protected override bool? GetRefreshSupport(ClientCapabilities clientCapabilities)
        => clientCapabilities.Workspace?.Diagnostics?.RefreshSupport;

    protected override string GetWorkspaceRefreshName()
        => Methods.WorkspaceDiagnosticRefreshName;
}
