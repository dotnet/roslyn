// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

#if !DOTNET_BUILD_FROM_SOURCE
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Remote;
#endif

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
        public static async Task<ISymbolSearchUpdateEngine> CreateEngineAsync(
            Workspace workspace,
            ISymbolSearchLogService logService,
            CancellationToken cancellationToken)
        {
#if !DOTNET_BUILD_FROM_SOURCE
            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                return new SymbolSearchUpdateEngineProxy(client, logService);
            }
#endif

            // Couldn't go out of proc.  Just do everything inside the current process.
            return CreateEngineInProcess();
        }

        /// <summary>
        /// This returns a No-op engine if called on non-Windows OS, because the backing storage depends on Windows APIs.
        /// </summary>
        public static ISymbolSearchUpdateEngine CreateEngineInProcess()
        {
#if !DOTNET_BUILD_FROM_SOURCE
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new SymbolSearchUpdateEngine();
#endif
            return NoOpUpdateEngine.Instance;
        }

        private sealed class NoOpUpdateEngine : ISymbolSearchUpdateEngine
        {
            public static readonly NoOpUpdateEngine Instance = new();

            public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
                => ValueTaskFactory.FromResult(ImmutableArray<PackageWithAssemblyResult>.Empty);

            public ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
                => ValueTaskFactory.FromResult(ImmutableArray<PackageWithTypeResult>.Empty);

            public ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
                => ValueTaskFactory.FromResult(ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty);

            public ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, ISymbolSearchLogService logService, CancellationToken cancellationToken)
                => default;
        }
    }
}
