// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#if DOTNET_BUILD_FROM_SOURCE

#pragma warning disable IDE0060 // Remove unused parameter

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    /// <remarks>
    /// Returns an No-op engine on non-Windows OS, because the backing storage depends on Windows APIs.
    /// </remarks>
    internal static class SymbolSearchUpdateEngineFactory
    {
        public static ValueTask<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace,
            ISymbolSearchLogService logService,
            CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult<ISymbolSearchUpdateEngine>(SymbolSearchUpdateNoOpEngine.Instance);
    }
}
#endif
