// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Analyzers;

internal static class OmniSharpWorkspaceAnalyzerOptionsFactory
{
#pragma warning disable IDE0060 // Remove unused parameter
    public static AnalyzerOptions Create(Solution solution, AnalyzerOptions options)
        => options;
#pragma warning restore IDE0060 // Remove unused parameter
}
