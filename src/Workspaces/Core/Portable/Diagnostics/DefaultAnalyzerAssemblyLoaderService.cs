// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceServiceFactory(typeof(IAnalyzerAssemblyLoaderProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultAnalyzerAssemblyLoaderServiceFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new DefaultAnalyzerAssemblyLoaderProvider(workspaceServices.Workspace.Kind ?? "default");

    private sealed class DefaultAnalyzerAssemblyLoaderProvider(string workspaceKind) : IAnalyzerAssemblyLoaderProvider
    {
        private readonly DefaultAnalyzerAssemblyLoader _loader = new();

        /// <summary>
        /// We include the <see cref="WorkspaceKind"/> of the workspace in the path we produce.  That way we don't
        /// collide in the common case of a normal host workspace and OOP workspace running together.  This avoids an
        /// annoying exception as each will try to clean up this directory, throwing exceptions because the other is
        /// locking it.  The exception is fine, since the cleanup is just hygenic and isn't intended to be needed for
        /// correctness.  But it is annoying and does cause noise in our perf test harness.
        /// </summary>
        private readonly IAnalyzerAssemblyLoader _shadowCopyLoader = DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
            Path.Combine(Path.GetTempPath(), "CodeAnalysis", "WorkspacesAnalyzerShadowCopies", workspaceKind));

        public IAnalyzerAssemblyLoader GetLoader(in AnalyzerAssemblyLoaderOptions options)
            => options.ShadowCopy ? _shadowCopyLoader : _loader;
    }
}
