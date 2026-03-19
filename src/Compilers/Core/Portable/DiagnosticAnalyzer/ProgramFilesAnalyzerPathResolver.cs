// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// This <see cref="IAnalyzerPathResolver"/> implementation is used to handle analyzers that
/// exist in global install locations on Windows. These locations do not need to be shadow
/// copied because they are read-only and are not expected to be updated. Putting this resolver
/// before shadow copy will let them load in place.
/// </summary>
#if NET
[SupportedOSPlatform("windows")]
#endif
internal sealed class ProgramFilesAnalyzerPathResolver : IAnalyzerPathResolver
{
    internal static readonly IAnalyzerPathResolver Instance = new ProgramFilesAnalyzerPathResolver();

    private string DotNetPath { get; }

    private ProgramFilesAnalyzerPathResolver()
    {
        var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        DotNetPath = Path.Combine(programFilesPath, "dotnet");
    }

    public bool IsAnalyzerPathHandled(string analyzerPath)
        => analyzerPath.StartsWith(DotNetPath, StringComparison.OrdinalIgnoreCase);

    public string GetResolvedAnalyzerPath(string originalAnalyzerPath)
    {
        Debug.Assert(IsAnalyzerPathHandled(originalAnalyzerPath));
        return originalAnalyzerPath;
    }

    public string? GetResolvedSatellitePath(string originalAnalyzerPath, CultureInfo cultureInfo)
    {
        Debug.Assert(IsAnalyzerPathHandled(originalAnalyzerPath));
        return AnalyzerAssemblyLoader.GetSatelliteAssemblyPath(originalAnalyzerPath, cultureInfo);
    }
}
