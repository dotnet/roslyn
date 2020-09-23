// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class DiagnosticAnalyzerExtensions
    {
        public static DiagnosticAnalyzerCategory GetDiagnosticAnalyzerCategory(this DiagnosticAnalyzer analyzer)
            => analyzer switch
            {
                FileContentLoadAnalyzer _ => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis,
                DocumentDiagnosticAnalyzer _ => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis,
                ProjectDiagnosticAnalyzer _ => DiagnosticAnalyzerCategory.ProjectAnalysis,
                IBuiltInAnalyzer builtInAnalyzer => builtInAnalyzer.GetAnalyzerCategory(),

                // It is not possible to know the categorization for a public analyzer, so return a worst-case categorization.
                _ => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis | DiagnosticAnalyzerCategory.ProjectAnalysis
            };

        public static bool SupportAnalysisKind(this DiagnosticAnalyzer analyzer, AnalysisKind kind)
        {
            // compiler diagnostic analyzer always supports all kinds:
            if (analyzer.IsCompilerAnalyzer())
            {
                return true;
            }

            return kind switch
            {
                AnalysisKind.Syntax => analyzer.SupportsSyntaxDiagnosticAnalysis(),
                AnalysisKind.Semantic => analyzer.SupportsSemanticDiagnosticAnalysis(),
                _ => throw ExceptionUtilities.UnexpectedValue(kind)
            };
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
