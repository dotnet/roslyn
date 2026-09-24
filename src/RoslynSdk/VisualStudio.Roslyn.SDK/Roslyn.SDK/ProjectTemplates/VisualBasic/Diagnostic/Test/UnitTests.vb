Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports VerifyVB = $safeprojectname$.VisualBasicCodeFixVerifier(
    Of $saferootprojectname$.$saferootidentifiername$Analyzer,
    $saferootprojectname$.$saferootidentifiername$CodeFixProvider)

Namespace $safeprojectname$
    <TestClass>
    Public Class $saferootidentifiername$UnitTest

        'No diagnostics expected to show up
        <TestMethod>
        Public Async Function TestMethod1() As Task
            Dim test = ""
            Await VerifyVB.VerifyAnalyzerAsync(test)
        End Function

        'Diagnostic And CodeFix both triggered And checked for
        <TestMethod>
        Public Async Function TestMethod2() As Task

            Dim test = "
Class {|#0:TypeName|}

    Sub Main()

    End Sub

End Class"

            Dim fixtest = "
Class TYPENAME

    Sub Main()

    End Sub

End Class"

            Dim expected = VerifyVB.Diagnostic("$saferootidentifiername$").WithLocation(0).WithArguments("TypeName")
            Await VerifyVB.VerifyCodeFixAsync(test, expected, fixtest)
        End Function
    End Class
End Namespace
