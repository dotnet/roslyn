// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Factory that creates the hot reload brokered service stack. Non-brokered dependencies are resolved via MEF;
/// brokered-service-dependent components and the host's <see cref="IHostWorkspaceProvider"/> are passed
/// to <see cref="CreateImplementation"/>.
/// </summary>
[Shared]
[Export(typeof(ManagedHotReloadLanguageServiceFactory))]
[Export(typeof(IEditAndContinueSolutionProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ManagedHotReloadLanguageServiceFactory(
    EditAndContinueSessionState sessionState,
    IActiveStatementTrackingController activeStatementTrackingController,
    IDiagnosticsRefresher diagnosticRefresher,
    IAsynchronousOperationListenerProvider listenerProvider) : IEditAndContinueSolutionProvider
{
    public static readonly ServiceRpcDescriptor ServiceDescriptor = IManagedHotReloadUpdatesProvider.CreateServiceDescriptor(ManagedHotReloadUpdatesProviderDescriptor.Moniker);

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
    public ManagedHotReloadLanguageServiceImpl CreateImplementation(
        IServiceBroker serviceBroker,
        ISolutionSnapshotProvider solutionSnapshotProvider,
        IHostWorkspaceProvider workspaceProvider,
        PdbMatchingSourceTextProvider sourceTextProvider)
    {
        var hotReloadStateProxy = new ManagedHotReloadStateProxy(serviceBroker);
        var logReporter = new EditAndContinueLogReporter(serviceBroker, listenerProvider);

        var impl = new ManagedHotReloadLanguageServiceImpl(
            sessionState,
            workspaceProvider,
            hotReloadStateProxy,
            solutionSnapshotProvider,
            sourceTextProvider,
            activeStatementTrackingController,
            logReporter,
            diagnosticRefresher);

        impl.SolutionCommitted += solution => SolutionCommitted?.Invoke(solution);
        return impl;
    }
}
