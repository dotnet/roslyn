// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Factory that creates the hot reload brokered service stack. Non-brokered dependencies are resolved via MEF;
/// brokered-service-dependent components are manually constructed with an <see cref="IServiceBroker"/> passed
/// to <see cref="Create"/>.
/// </summary>
[Shared]
[Export(typeof(ManagedHotReloadLanguageServiceFactory))]
[Export(typeof(IEditAndContinueSolutionProvider))]
internal sealed class ManagedHotReloadLanguageServiceFactory : IEditAndContinueSolutionProvider
{
    private readonly EditAndContinueSessionState _sessionState;
    private readonly Lazy<IHostWorkspaceProvider> _workspaceProvider;
    private readonly PdbMatchingSourceTextProvider _sourceTextProvider;
    private readonly IActiveStatementTrackingController _activeStatementTrackingController;
    private readonly IDiagnosticsRefresher _diagnosticRefresher;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ManagedHotReloadLanguageServiceFactory(
        EditAndContinueSessionState sessionState,
        Lazy<IHostWorkspaceProvider> workspaceProvider,
        PdbMatchingSourceTextProvider sourceTextProvider,
        IActiveStatementTrackingController activeStatementTrackingController,
        IDiagnosticsRefresher diagnosticRefresher,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _sessionState = sessionState;
        _workspaceProvider = workspaceProvider;
        _sourceTextProvider = sourceTextProvider;
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
    public ManagedHotReloadLanguageService Create(
        IServiceBroker serviceBroker,
        ISolutionSnapshotProvider solutionSnapshotProvider)
    {
        var debuggerServiceProxy = new ManagedHotReloadServiceProxy(serviceBroker);
        var logReporter = new EditAndContinueLogReporter(serviceBroker, _listenerProvider);

        var impl = new ManagedHotReloadLanguageServiceImpl(
            _sessionState,
            _workspaceProvider,
            debuggerServiceProxy,
            solutionSnapshotProvider,
            _sourceTextProvider,
            _activeStatementTrackingController,
            logReporter,
            _diagnosticRefresher);

        impl.SolutionCommitted += solution => SolutionCommitted?.Invoke(solution);

        return new ManagedHotReloadLanguageService(impl);
    }
}
