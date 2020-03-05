// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerDriver
    {
        private static class AnalyzerConfigOptionNames
        {
            private const string DotnetAnalyzerDiagnosticPrefix = "dotnet_analyzer_diagnostic";
            private const string CategoryPrefix = "category";
            private const string SeveritySuffix = "severity";

            public const string DotnetAnalyzerDiagnosticSeverityKey = DotnetAnalyzerDiagnosticPrefix + "." + SeveritySuffix;

            public static string GetCategoryBasedDotnetAnalyzerDiagnosticSeverityKey(string category)
                => $"{DotnetAnalyzerDiagnosticPrefix}.{CategoryPrefix}-{category}.{SeveritySuffix}";
        }
    }
}
