Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing
Imports Microsoft.CodeAnalysis.Testing.Verifiers
Imports Microsoft.CodeAnalysis.VisualBasic.Testing

Partial Public NotInheritable Class VisualBasicCodeFixVerifier(Of TAnalyzer As {DiagnosticAnalyzer, New}, TCodeFix As {CodeFixProvider, New})

    Private Sub New()
        Throw New NotSupportedException()
    End Sub

    ''' <inheritdoc cref="CodeFixVerifier(Of TAnalyzer, TCodeFix, TTest, TVerifier).Diagnostic()"/>
    Public Shared Function Diagnostic() As DiagnosticResult
        Return VisualBasicCodeFixVerifier(Of TAnalyzer, TCodeFix, MSTestVerifier).Diagnostic()
    End Function

    ''' <inheritdoc cref="CodeFixVerifier(Of TAnalyzer, TCodeFix, TTest, TVerifier).Diagnostic(String)"/>
    Public Shared Function Diagnostic(diagnosticId As String) As DiagnosticResult
        Return VisualBasicCodeFixVerifier(Of TAnalyzer, TCodeFix, MSTestVerifier).Diagnostic(diagnosticId)
    End Function

    ''' <inheritdoc cref="CodeFixVerifier(Of TAnalyzer, TCodeFix, TTest, TVerifier).Diagnostic(DiagnosticDescriptor)"/>
    Public Shared Function Diagnostic(descriptor As DiagnosticDescriptor) As DiagnosticResult
        Return VisualBasicCodeFixVerifier(Of TAnalyzer, TCodeFix, MSTestVerifier).Diagnostic(descriptor)
    End Function

    ''' <inheritdoc cref="CodeFixVerifier(Of TAnalyzer, TCodeFix, TTest, TVerifier).VerifyAnalyzerAsync(String, DiagnosticResult())"/>
    Public Shared Async Function VerifyAnalyzerAsync(source As String, ParamArray expected As DiagnosticResult()) As Task
        Dim test As New Test With
        {
        .TestCode = source
        }

        test.ExpectedDiagnostics.AddRange(expected)
        Await test.RunAsync(CancellationToken.None)
    End Function

    ''' <inheritdoc cref="CodeFixVerifier(Of TAnalyzer, TCodeFix, TTest, TVerifier).VerifyCodeFixAsync(String, String)"/>
    Public Shared Async Function VerifyCodeFixAsync(source As String, fixedSource As String) As Task
        Await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource)
    End Function

    ''' <inheritdoc cref="CodeFixVerifier(Of TAnalyzer, TCodeFix, TTest, TVerifier).VerifyCodeFixAsync(String, DiagnosticResult, String)"/>
    Public Shared Async Function VerifyCodeFixAsync(source As String, expected As DiagnosticResult, fixedSource As String) As Task
        Await VerifyCodeFixAsync(source, {expected}, fixedSource)
    End Function

    ''' <inheritdoc cref="CodeFixVerifier(Of TAnalyzer, TCodeFix, TTest, TVerifier).VerifyCodeFixAsync(String, DiagnosticResult(), String)"/>
    Public Shared Async Function VerifyCodeFixAsync(source As String, expected As DiagnosticResult(), fixedSource As String) As Task
        Dim test As New Test With
        {
        .TestCode = source,
        .FixedCode = fixedSource
        }

        test.ExpectedDiagnostics.AddRange(expected)
        Await test.RunAsync(CancellationToken.None)
    End Function

End Class
