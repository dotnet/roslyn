// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Remote
{
    internal interface IRemoteProcessTelemetryService
    {
        /// <summary>
        /// Enables logging of <paramref name="functionIds"/> using loggers of the specified <paramref name="loggerTypeNames"/>.
        /// </summary>
        ValueTask EnableLoggingAsync(ImmutableArray<string> loggerTypeNames, ImmutableArray<FunctionId> functionIds, CancellationToken cancellationToken);

        /// <summary>
        /// Initializes telemetry session.
        /// </summary>
        ValueTask InitializeTelemetrySessionAsync(int hostProcessId, string serializedSession, bool logDelta, CancellationToken cancellationToken);

        /// <summary>
        /// Sets <see cref="WorkspaceConfigurationOptions"/> for the process.
        /// Called as soon as the remote process is created but can't guarantee that solution entities (projects, documents, syntax trees) have not been created beforehand.
        /// </summary>
        /// <returns>Process ID of the remote process.</returns>
        ValueTask<int> InitializeAsync(WorkspaceConfigurationOptions options, CancellationToken cancellationToken);
    }
}
