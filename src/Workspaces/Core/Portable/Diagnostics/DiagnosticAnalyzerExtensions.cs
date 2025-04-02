// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static partial class DiagnosticAnalyzerExtensions
{
    public static DiagnosticAnalyzerCategory GetDiagnosticAnalyzerCategory(this DiagnosticAnalyzer analyzer)
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

    public static bool SupportAnalysisKind(this DiagnosticAnalyzer analyzer, AnalysisKind kind)
        => kind switch
        {
            AnalysisKind.Syntax => analyzer.SupportsSyntaxDiagnosticAnalysis(),
            AnalysisKind.Semantic => analyzer.SupportsSemanticDiagnosticAnalysis(),
            _ => throw ExceptionUtilities.UnexpectedValue(kind)
        };

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
}
