// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
