// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface IClientProcessMonitor : ILspService
{
    /// <summary>
    /// Gets the process ID whose lifetime controls this logical language server, or <see langword="null"/> when
    /// no process should be monitored.
    /// </summary>
    int? GetClientProcessId();

    /// <summary>
    /// Gets the action to take when the monitored process exits or can no longer be monitored.
    /// </summary>
    ShutdownStrategy Strategy { get; }

    /// <summary>
    /// The action to take after the monitored client process exits.
    /// </summary>
    public enum ShutdownStrategy
    {
        /// <summary>
        /// Terminates the entire language server process. Used when the process hosts only one logical server.
        /// </summary>
        ProcessExit,

        /// <summary>
        /// Shuts down only the associated logical language server. Used by a daemon hosting multiple clients.
        /// </summary>
        LSPShutdown
    }
}
