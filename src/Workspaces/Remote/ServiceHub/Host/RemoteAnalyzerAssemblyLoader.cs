// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Redirecting;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics;

/// <summary>
/// For analyzers shipped in Roslyn, different set of assemblies might be used when running
/// in-proc and OOP e.g. in-proc (VS) running on desktop clr and OOP running on ServiceHub .Net6
/// host. We need to make sure to use the ones from the same location as the remote.
/// </summary>
internal sealed class RemoteAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
{
    private readonly string _baseDirectory;

    public RemoteAnalyzerAssemblyLoader(string baseDirectory, ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers = default, ImmutableArray<IAnalyzerAssemblyRedirector> externalRedirectors = default)
        : base(externalResolvers, externalRedirectors)
    {
        _baseDirectory = baseDirectory;
    }

    protected override string PreparePathToLoad(string fullPath)
    {
        var fixedPath = Path.GetFullPath(Path.Combine(_baseDirectory, Path.GetFileName(fullPath)));
        return File.Exists(fixedPath) ? fixedPath : fullPath;
    }

    protected override string PrepareSatelliteAssemblyToLoad(string fullPath, string cultureName)
    {
        var fixedPath = Path.GetFullPath(Path.Combine(_baseDirectory, cultureName, Path.GetFileName(fullPath)));
        return File.Exists(fixedPath) ? fixedPath : fullPath;
    }
}
