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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "False Positive asking to add TopString and =Operator")]
    public readonly record struct ProcessInfo
    {
        public ProcessInfo(Guid processId, uint? localProcessId, string? path)
        {
            ProcessId = processId;
            LocalProcessId = localProcessId;
            Path = path;
        }
        public readonly Guid ProcessId { get; }
        public readonly uint? LocalProcessId { get; }
        public readonly string? Path { get; }
    }

    public interface IVisualDiagnosticsLanguageService : IWorkspaceService
    {
        public Task InitializeAsync(IServiceBroker serviceBroker, CancellationToken token);
        public Task StartDebuggingSessionAsync(ProcessInfo info, CancellationToken token);
        public Task StopDebuggingSessionAsync(ProcessInfo info, CancellationToken token);
    }
}
