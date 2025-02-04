// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// This <see cref="IAnalyzerPathResolver"/> implementation is used to handle analyzers that
/// exist in global install locations on Windows. These locations do not need to be shadow
/// copied because they are read-only and are not expected to be updated. Putting this resolver
/// before shadow copy will let them load in place.
/// </summary>
internal sealed class ProgramFilesAnalyzerPathResolver : IAnalyzerPathResolver
{
    public string ProgramFilesPath { get; }
    public string DotNetPath { get; }
    public string VisualStudioPath { get; }

    public ProgramFilesAnalyzerPathResolver()
    {
        ProgramFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        DotNetPath = Path.Combine(ProgramFilesPath, "dotnet");
        VisualStudioPath = Path.Combine(ProgramFilesPath, "Microsoft Visual Studio");
    }

    public bool IsAnalyzerPathHandled(string analyzerPath)
        => analyzerPath.StartsWith(DotNetPath, StringComparison.OrdinalIgnoreCase) ||
           analyzerPath.StartsWith(VisualStudioPath, StringComparison.OrdinalIgnoreCase);

    public string GetRealAnalyzerPath(string analyzerPath)
    {
        Debug.Assert(IsAnalyzerPathHandled(analyzerPath));
        return analyzerPath;
    }

    public string? GetRealSatellitePath(string analyzerPath, CultureInfo cultureInfo)
    {
        Debug.Assert(IsAnalyzerPathHandled(analyzerPath));
        return AnalyzerAssemblyLoader.GetSatelliteAssemblyPath(analyzerPath, cultureInfo);
    }
}
