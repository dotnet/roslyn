// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class DiagnosticAnalyzerExtensions
    {
        public static bool SupportsSyntaxDiagnosticAnalysis(this DiagnosticAnalyzer analyzer)
        {
            return !(analyzer is ProjectDiagnosticAnalyzer);
        }

        public static bool SupportsSemanticDiagnosticAnalysis(this DiagnosticAnalyzer analyzer)
        {
            return !(analyzer is ProjectDiagnosticAnalyzer);
        }

        public static bool SupportsSpanBasedSemanticDiagnosticAnalysis(this DiagnosticAnalyzer analyzer)
        {
            return false;
        }

        public static bool SupportsProjectDiagnosticAnalysis(this DiagnosticAnalyzer analyzer)
        {
            return !(analyzer is DocumentDiagnosticAnalyzer);
        }
    }
}
