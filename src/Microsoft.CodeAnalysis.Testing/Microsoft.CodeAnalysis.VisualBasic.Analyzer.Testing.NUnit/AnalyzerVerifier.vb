Imports Microsoft.CodeAnalysis.Diagnostics

Module AnalyzerVerifier
    Function Create(Of TAnalyzer As {DiagnosticAnalyzer, New})() As AnalyzerVerifier(Of TAnalyzer)
        Return New AnalyzerVerifier(Of TAnalyzer)
    End Function
End Module
