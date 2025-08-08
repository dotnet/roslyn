// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.MSBuild;

public partial class MSBuildProjectLoader
{
    private sealed partial class Worker
    {
        private sealed class AnalyzerReferencePathComparer : IEqualityComparer<AnalyzerReference?>
        {
            public static AnalyzerReferencePathComparer Instance = new();

            private AnalyzerReferencePathComparer() { }

            public bool Equals(AnalyzerReference? x, AnalyzerReference? y)
                => string.Equals(x?.FullPath, y?.FullPath, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(AnalyzerReference? obj)
                => obj?.FullPath?.GetHashCode() ?? 0;
        }
    }
}
