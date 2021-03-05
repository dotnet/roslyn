Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Testing

Partial Public NotInheritable Class VisualBasicAnalyzerVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New})
    Public Class Test
        Inherits VisualBasicAnalyzerTest(Of TAnalyzer, MSTestVerifier)

        Public Sub New()
        End Sub
    End Class
End Class
