// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Collections.Immutable;
using System.Collections.Generic;

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceServiceFactory(typeof(IAnalyzerAssemblyLoaderProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultAnalyzerAssemblyLoaderServiceFactory(
    [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new DefaultAnalyzerAssemblyLoaderProvider(workspaceServices.Workspace.Kind ?? "default", [.. externalResolvers]);

    private sealed class DefaultAnalyzerAssemblyLoaderProvider(string workspaceKind, ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
        : IAnalyzerAssemblyLoaderProvider
    {
        /// <summary>
        /// We include the <see cref="WorkspaceKind"/> of the workspace in the path we produce.  That way we don't
        /// collide in the common case of a normal host workspace and OOP workspace running together.  This avoids an
        /// annoying exception as each will try to clean up this directory, throwing exceptions because the other is
        /// locking it.  The exception is fine, since the cleanup is just hygienic and isn't intended to be needed for
        /// correctness.  But it is annoying and does cause noise in our perf test harness.
        /// </summary>
        private readonly IAnalyzerAssemblyLoader _shadowCopyLoader = DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
#if NET
            loadContext: null,
#endif
            GetPath(workspaceKind),
            externalResolvers);

        private static string GetPath(string workspaceKind)
            => Path.Combine(Path.GetTempPath(), "CodeAnalysis", "WorkspacesAnalyzerShadowCopies", workspaceKind);

#if NET

        public IAnalyzerAssemblyLoader GetShadowCopyLoader(AssemblyLoadContext? loadContext)
            => loadContext is null
                ? _shadowCopyLoader
                : DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(loadContext, GetPath(workspaceKind));

#else

        public IAnalyzerAssemblyLoader GetShadowCopyLoader()
            => _shadowCopyLoader;

#endif
    }
}
