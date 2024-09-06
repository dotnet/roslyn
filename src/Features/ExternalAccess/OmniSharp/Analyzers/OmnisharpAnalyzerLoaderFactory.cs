// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Analyzers
{
    internal static class OmnisharpAnalyzerAssemblyLoaderFactory
    {
        public static IAnalyzerAssemblyLoader CreateShadowCopyAnalyzerAssemblyLoader(string? baseDirectory = null)
        {
            baseDirectory ??= Path.Combine(Path.GetTempPath(), "CodeAnalysis", "OmnisharpAnalyzerShadowCopies");
            return DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(baseDirectory);
        }
    }
}
