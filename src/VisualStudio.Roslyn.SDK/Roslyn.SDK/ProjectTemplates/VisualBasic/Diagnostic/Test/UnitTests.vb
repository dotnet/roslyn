Imports $saferootprojectname$
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports System.Threading
Imports System.Threading.Tasks
Imports Verify = Microsoft.CodeAnalysis.VisualBasic.Testing.MSTest.CodeFixVerifier(
    Of $saferootprojectname$.$saferootidentifiername$Analyzer,
    $saferootprojectname$.$saferootidentifiername$CodeFixProvider)

Namespace $safeprojectname$
    <TestClass>
    Public Class UnitTest

        'No diagnostics expected to show up
        <TestMethod>
        Public Async Function TestMethod1() As Task
            Dim test = ""
            Await Verify.VerifyAnalyzerAsync(test)
        End Function

        'Diagnostic And CodeFix both triggered And checked for
        <TestMethod>
        Public Async Function TestMethod2() As Task

            Dim test = "
Module Module1

    Sub Main()

    End Sub

End Module"

            Dim fixtest = "
Module MODULE1

    Sub Main()

    End Sub

End Module"

            Dim expected = Verify.Diagnostic("$saferootidentifiername$").WithLocation(2, 8).WithArguments("Module1")
            Await Verify.VerifyCodeFixAsync(test, expected, fixtest)
        End Function

    End Class
End Namespace
