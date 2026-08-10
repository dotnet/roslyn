// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Factory that creates the hot reload brokered service stack. Non-brokered dependencies are resolved via MEF;
/// brokered-service-dependent components and the host's <see cref="IHostWorkspaceProvider"/> are passed
/// to <see cref="Create"/>.
/// </summary>
[Shared]
[Export(typeof(ManagedHotReloadLanguageServiceFactory))]
[Export(typeof(IEditAndContinueSolutionProvider))]
internal sealed class ManagedHotReloadLanguageServiceFactory : IEditAndContinueSolutionProvider
{
    private readonly EditAndContinueSessionState _sessionState;
    private readonly IActiveStatementTrackingController _activeStatementTrackingController;
    private readonly IDiagnosticsRefresher _diagnosticRefresher;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ManagedHotReloadLanguageServiceFactory(
        EditAndContinueSessionState sessionState,
        IActiveStatementTrackingController activeStatementTrackingController,
        IDiagnosticsRefresher diagnosticRefresher,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _sessionState = sessionState;
        _activeStatementTrackingController = activeStatementTrackingController;
        _diagnosticRefresher = diagnosticRefresher;
        _listenerProvider = listenerProvider;
    }

    public event Action<Solution>? SolutionCommitted;

    /// <summary>
    /// Creates the hot reload brokered service stack.
    /// </summary>
    /// <param name="serviceBroker">The service broker used to acquire debugger and logger services.</param>
    /// <param name="solutionSnapshotProvider">
    /// Host-specific solution snapshot provider. In VS this is EditorHostSolutionProvider (from MEF)
    /// </param>
    /// <param name="workspaceProvider">Host's workspace provider used by the implementation to enqueue source generator updates.</param>
    /// <param name="sourceTextProvider">
    /// Per-host source text provider, observing the host's <see cref="WorkspaceKind.Host"/> workspace. Owned by the caller
    /// (the per-server hot reload stack), which is responsible for disposing it when the host/server shuts down.
    /// </param>
    public ManagedHotReloadLanguageService Create(
        IServiceBroker serviceBroker,
        ISolutionSnapshotProvider solutionSnapshotProvider,
        IHostWorkspaceProvider workspaceProvider,
        PdbMatchingSourceTextProvider sourceTextProvider)
    {
        var debuggerServiceProxy = new ManagedHotReloadServiceProxy(serviceBroker);
        var logReporter = new EditAndContinueLogReporter(serviceBroker, _listenerProvider);

        var impl = new ManagedHotReloadLanguageServiceImpl(
            _sessionState,
            workspaceProvider,
            debuggerServiceProxy,
            solutionSnapshotProvider,
            sourceTextProvider,
            _activeStatementTrackingController,
            logReporter,
            _diagnosticRefresher);

        impl.SolutionCommitted += solution => SolutionCommitted?.Invoke(solution);

        return new ManagedHotReloadLanguageService(impl);
    }
}
