Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicAnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TVerifier As {IVerifier, New})
    Inherits AnalyzerVerifier(Of TAnalyzer, VisualBasicAnalyzerTest(Of TAnalyzer, TVerifier), TVerifier)
End Class
