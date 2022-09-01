// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <summary>
    /// Factory that will produce the <see cref="ISymbolSearchUpdateEngine"/>.  The default
    /// implementation produces an engine that will run in-process.  Implementations at
    /// other layers can behave differently (for example, running the engine out-of-process).
    /// </summary>
    /// <remarks>
    /// This returns an No-op engine on non-Windows OS, because the backing storage depends on Windows APIs.
    /// </remarks>
    internal static class SymbolSearchUpdateEngineFactory
    {
        public static async ValueTask<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace,
            ISymbolSearchLogService logService,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                return new SymbolSearchUpdateEngineProxy(client, logService);
            }

            // Couldn't go out of proc.  Just do everything inside the current process.
            return CreateEngineInProcess();
        }

        /// <summary>
        /// This returns a No-op engine if called on non-Windows OS, because the backing storage depends on Windows APIs.
        /// </summary>
        public static ISymbolSearchUpdateEngine CreateEngineInProcess()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                new SymbolSearchUpdateEngine() :
                SymbolSearchUpdateNoOpEngine.Instance;
        }
    }
}
