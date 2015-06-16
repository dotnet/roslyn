Namespace BasicAnalyzers
    Public Module DiagnosticIds
        ' Stateless analyzer IDs.
        Public Const SymbolAnalyzerRuleId As String = "VBS0001"
        Public Const SyntaxNodeAnalyzerRuleId As String = "VBS0002"
        Public Const SyntaxTreeAnalyzerRuleId As String = "VBS0003"
        Public Const SemanticModelAnalyzerRuleId As String = "VBS0004"
        Public Const CodeBlockAnalyzerRuleId As String = "VBS0005"
        Public Const CompilationAnalyzerRuleId As String = "VBS0006"

        ' Stateful analyzer IDs.
        Public Const CodeBlockStartedAnalyzerRuleId As String = "VBS0101"
        Public Const CompilationStartedAnalyzerRuleId As String = "VBS0102"
        Public Const CompilationStartedAnalyzerWithCompilationWideAnalysisRuleId As String = "VBS0103"
    End Module
End Namespace
