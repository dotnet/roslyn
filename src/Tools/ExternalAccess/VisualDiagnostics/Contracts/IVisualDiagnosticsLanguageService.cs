// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts
{
    /// <summary>
    /// Process Information
    /// </summary>
    /// <param name="ProcessId">Unique GUID that uniquely identify a process under a debug session</param>
    /// <param name="LocalProcessId">local process running on a host device, if the process running on a different host (like mobile device), this will be null</param>
    /// <param name="Path">path to the process, this is guaranteed to not be null for local process, but could be null for mobile devices</param>
    /// <param name="ConnectionId">Connection Id identifying a data bridge connection to this process</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0051:Add internal types and members to the declared API", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7237")]
    internal record struct ProcessInfo(Guid ProcessId, uint? LocalProcessId, string? Path, string? ConnectionId);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0051:Add internal types and members to the declared API", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/7237")]
    internal record struct ConnectionInfo(string? ConnectionId, string? Handle, string? Address, uint? PortNumber);

    /// <summary>
    /// Workspace service responsible for starting a Visual Diagnostic session on the LSP server
    /// </summary>
    internal interface IVisualDiagnosticsLanguageService : IWorkspaceService
    {
        /// <summary>
        /// Initialize the diagnostic host
        /// </summary>
        /// <param name="serviceBroker">Service broker</param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        public Task InitializeAsync(IServiceBroker serviceBroker, CancellationToken token);

        /// <summary>
        /// Request the creation of a data bridge connection info given a connection Id 
        /// </summary>
        /// <param name="connectionId">Unique identifiable token for a given connection</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>a ConnectionInfo record  <seealso cref="ConnectionInfo"/></returns>
        public Task<ConnectionInfo> RequestDataBridgeConnectionAsync(string connectionId, CancellationToken token);

        /// <summary>
        /// Notifies the diagnostic host workspace service that a debugging session has started.
        /// </summary>
        /// <param name="info">Process information <seealso cref="ProcessInfo"/></param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        public Task HandleDiagnosticSessionStartAsync(ProcessInfo info, CancellationToken token);

        /// <summary>
        /// Notifies the diagnostic host workspace service that a debugging session has ended.
        /// </summary>
        /// <param name="info">Process information <seealso cref="ProcessInfo"/></param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        public Task HandleDiagnosticSessionStopAsync(ProcessInfo info, CancellationToken token);
    }
}
