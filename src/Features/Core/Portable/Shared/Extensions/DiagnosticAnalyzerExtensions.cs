// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class DiagnosticAnalyzerExtensions
    {
        public static DiagnosticAnalyzerCategory GetDiagnosticAnalyzerCategory(this DiagnosticAnalyzer analyzer)
        {
            var category = DiagnosticAnalyzerCategory.None;

            if (analyzer is DocumentDiagnosticAnalyzer)
            {
                category |= DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;
            }
            else if (analyzer is ProjectDiagnosticAnalyzer)
            {
                category |= DiagnosticAnalyzerCategory.ProjectAnalysis;
            }
            else
            {
                if (analyzer is IBuiltInAnalyzer builtInAnalyzer)
                {
                    category = builtInAnalyzer.GetAnalyzerCategory();
                }
                else
                {
                    // It is not possible to know the categorization for a public analyzer,
                    // so return a worst-case categorization.
                    category = (DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis | DiagnosticAnalyzerCategory.ProjectAnalysis);
                }
            }

            return category;
        }

        public static bool SupportsSyntaxDiagnosticAnalysis(this DiagnosticAnalyzer analyzer)
        {
            var category = analyzer.GetDiagnosticAnalyzerCategory();
            return (category & DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis) != 0;
        }

        public static bool SupportsSemanticDiagnosticAnalysis(this DiagnosticAnalyzer analyzer)
        {
            var category = analyzer.GetDiagnosticAnalyzerCategory();
            return (category & (DiagnosticAnalyzerCategory.SemanticSpanAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis)) != 0;
        }

        public static bool SupportsSpanBasedSemanticDiagnosticAnalysis(this DiagnosticAnalyzer analyzer)
        {
            var category = analyzer.GetDiagnosticAnalyzerCategory();
            return (category & DiagnosticAnalyzerCategory.SemanticSpanAnalysis) != 0;
        }

        public static bool SupportsProjectDiagnosticAnalysis(this DiagnosticAnalyzer analyzer)
        {
            var category = analyzer.GetDiagnosticAnalyzerCategory();
            return (category & DiagnosticAnalyzerCategory.ProjectAnalysis) != 0;
        }
    }
}
