// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics;

/// <summary>
/// Customizes the path where to store shadow-copies of analyzer assemblies.
/// </summary>
[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider), [WorkspaceKind.RemoteWorkspace]), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RemoteAnalyzerAssemblyLoaderService(
    [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    : IAnalyzerAssemblyLoaderProvider
{
    /// <summary>
    /// Default shared instance, for all callers who do not want to provide a custom AssemblyLoadContext.
    /// </summary>
    public IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader { get; } = CreateLoader(externalResolvers);

    private static ShadowCopyAnalyzerAssemblyLoader CreateLoader(IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
        // Note: we use a different path here than what is used on the host.  First, using the same path wouldn't
        // actually provide any benefits, as the shadow copy system already always makes a unique guid-based directory
        // under the base directory passed in.  Second, every fresh loader attempts to delete the other directories off
        // of this base as a cleanup pass.  We don't want the host or the remote side cleaning up the other as each
        // already handles that, and this just ends up with more IO exceptions trying to cleanup locked directories.
        => new(Path.Combine(Path.GetTempPath(), "Remote", "AnalyzerAssemblyLoader"), externalResolvers.ToImmutableArray());

    public IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader()
        => CreateLoader(externalResolvers);
}
