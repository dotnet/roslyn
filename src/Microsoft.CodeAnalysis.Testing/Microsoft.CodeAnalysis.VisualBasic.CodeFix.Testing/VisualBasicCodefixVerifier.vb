Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicCodeFixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New}, TVerifier As {IVerifier, New})
    Inherits CodeFixVerifier(Of TAnalyzer, TCodeFix, VisualBasicCodeFixTest(Of TAnalyzer, TCodeFix, TVerifier), TVerifier)
End Class
