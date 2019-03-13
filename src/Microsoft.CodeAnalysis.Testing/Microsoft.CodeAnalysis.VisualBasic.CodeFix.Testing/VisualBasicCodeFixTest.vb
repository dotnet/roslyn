Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing

Public Class VisualBasicCodeFixTest(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New}, TVerifier As {IVerifier, New})
    Inherits CodeFixTest(Of TVerifier)

    Public Overrides ReadOnly Property Language As String
        Get
            Return LanguageNames.VisualBasic
        End Get
    End Property

    Protected Overrides ReadOnly Property DefaultFileExt As String
        Get
            Return "vb"
        End Get
    End Property

    Protected Overrides Function CreateCompilationOptions() As CompilationOptions
        Return New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
    End Function

    Protected Overrides Function GetCodeFixProviders() As IEnumerable(Of CodeFixProvider)
        Return New TCodeFix() {New TCodeFix()}
    End Function

    Protected Overrides Function GetDiagnosticAnalyzers() As IEnumerable(Of DiagnosticAnalyzer)
        Return New TAnalyzer() {New TAnalyzer()}
    End Function
End Class
