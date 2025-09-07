// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics;

/// <summary>
/// For analyzers shipped in Roslyn, different set of assemblies might be used when running
/// in-proc and OOP e.g. in-proc (VS) running on desktop clr and OOP running on ServiceHub .Net6
/// host. We need to make sure to use the ones from the same location as the remote.
/// </summary>
internal sealed class RemoteAnalyzerPathResolver(string baseDirectory) : IAnalyzerPathResolver
{
    private readonly string _baseDirectory = baseDirectory;

    private string GetFixedPath(string analyzerPath)
        => Path.GetFullPath(Path.Combine(_baseDirectory, Path.GetFileName(analyzerPath)));

    public bool IsAnalyzerPathHandled(string originalAnalyzerPath)
        => File.Exists(GetFixedPath(originalAnalyzerPath));

    public string GetResolvedAnalyzerPath(string originalAnalyzerPath)
        => GetFixedPath(originalAnalyzerPath);

    public string? GetResolvedSatellitePath(string originalAnalyzerPath, CultureInfo cultureInfo)
        => AnalyzerAssemblyLoader.GetSatelliteAssemblyPath(GetFixedPath(originalAnalyzerPath), cultureInfo);
}
