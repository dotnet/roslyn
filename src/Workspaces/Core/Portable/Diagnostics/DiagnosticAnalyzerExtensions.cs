// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static partial class DiagnosticAnalyzerExtensions
{
    extension(DiagnosticAnalyzer analyzer)
    {
        public DiagnosticAnalyzerCategory GetDiagnosticAnalyzerCategory()
        => analyzer switch
        {
            FileContentLoadAnalyzer => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis,
            DocumentDiagnosticAnalyzer => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis,
            IBuiltInAnalyzer builtInAnalyzer => builtInAnalyzer.GetAnalyzerCategory(),

            // Compiler analyzer supports syntax diagnostics, span-based semantic diagnostics and project level diagnostics.
            // For a public analyzer it is not possible to know the diagnostic categorization, so return a worst-case categorization.
            _ => analyzer.IsCompilerAnalyzer()
                ? DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis | DiagnosticAnalyzerCategory.SemanticSpanAnalysis
                : DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis,
        };

        public bool SupportAnalysisKind(AnalysisKind kind)
            => kind switch
            {
                AnalysisKind.Syntax => analyzer.SupportsSyntaxDiagnosticAnalysis(),
                AnalysisKind.Semantic => analyzer.SupportsSemanticDiagnosticAnalysis(),
                _ => throw ExceptionUtilities.UnexpectedValue(kind)
            };

        public bool SupportsSyntaxDiagnosticAnalysis()
        {
            var category = analyzer.GetDiagnosticAnalyzerCategory();
            return (category & DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis) != 0;
        }

        public bool SupportsSemanticDiagnosticAnalysis()
        {
            var category = analyzer.GetDiagnosticAnalyzerCategory();
            return (category & (DiagnosticAnalyzerCategory.SemanticSpanAnalysis | DiagnosticAnalyzerCategory.SemanticDocumentAnalysis)) != 0;
        }

        public bool SupportsSpanBasedSemanticDiagnosticAnalysis()
        {
            var category = analyzer.GetDiagnosticAnalyzerCategory();
            return (category & DiagnosticAnalyzerCategory.SemanticSpanAnalysis) != 0;
        }
    }
}
