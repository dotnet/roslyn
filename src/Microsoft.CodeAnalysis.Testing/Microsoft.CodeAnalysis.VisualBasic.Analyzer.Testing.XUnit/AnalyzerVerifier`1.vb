Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Testing

Public Class AnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Inherits VisualBasicAnalyzerVerifier(Of TAnalyzer, XUnitVerifier)
End Class
