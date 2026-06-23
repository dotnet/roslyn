// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteWorkspaceProvider : IWorkspaceProvider
{
    public static RemoteWorkspaceProvider Instance = new();

    /// <summary>
    /// Gets the remote workspace used in the Roslyn OOP process
    /// </summary>
    /// <remarks>
    /// Normally getting a workspace is possible from a document, project or solution snapshot but in the Roslyn OOP
    /// process that is explicitly denied via an exception. This method serves as a workaround when a workspace is
    /// needed (eg, the Go To Definition API requires one).
    ///
    /// This should be used sparingly and carefully, and no updates should be made to the workspace.
    /// </remarks>
    public Workspace GetWorkspace()
        => RemoteWorkspaceManager.Default.GetWorkspace();

    /// <summary>
    /// Exposes remote export-provider initialization for Razor tests.
    /// </summary>
    public static class TestAccessor
    {
        private static readonly SemaphoreSlim s_initializeGate = new(initialCount: 1, maxCount: 1);
        private static Func<string, TraceSource, CancellationToken, Task<string?>> s_initializeRemoteExportProviderBuilderAsync = RemoteExportProviderBuilder.InitializeAsync;
        private static bool s_initialized;
        private static string? s_errorMessages;

        public static async Task<string?> InitializeRemoteExportProviderBuilderAsync(string localSettingsDirectory, TraceSource traceLogger, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref s_initialized))
            {
                return s_errorMessages;
            }

            using var _ = await s_initializeGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false);

            if (Volatile.Read(ref s_initialized))
            {
                return s_errorMessages;
            }

            s_errorMessages = await s_initializeRemoteExportProviderBuilderAsync(localSettingsDirectory, traceLogger, cancellationToken).ConfigureAwait(false);
            Volatile.Write(ref s_initialized, true);

            return s_errorMessages;
        }

        public static void SetInitializeRemoteExportProviderBuilder(Func<string, TraceSource, CancellationToken, Task<string?>> initializeAsync)
        {
            s_initializeRemoteExportProviderBuilderAsync = initializeAsync;
        }

        public static void ResetInitializeRemoteExportProviderBuilder()
        {
            s_initializeRemoteExportProviderBuilderAsync = RemoteExportProviderBuilder.InitializeAsync;
            s_errorMessages = null;
            Volatile.Write(ref s_initialized, false);
        }
    }
}
