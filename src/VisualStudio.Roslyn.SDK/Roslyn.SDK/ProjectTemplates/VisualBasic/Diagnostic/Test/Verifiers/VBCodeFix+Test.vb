Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing.Verifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Testing

Partial Public NotInheritable Class VisualBasicCodeFixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New})
    Public Class Test
        Inherits VisualBasicCodeFixTest(Of TAnalyzer, TCodeFix, MSTestVerifier)

    End Class
End Class
