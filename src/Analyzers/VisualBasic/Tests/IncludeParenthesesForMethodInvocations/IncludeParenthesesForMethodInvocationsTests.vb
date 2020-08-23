' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.ParenthesesForMethodInvocations.VisualBasicAddParenthesesAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.IncludeParenthesesForMethodInvocations.VisualBasicIncludeParenthesesForMethodInvocationsCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.IncludeParenthesesForMethodInvocations
    Public Class IncludeParenthesesForMethodInvocationsTests
        Private Shared Async Function VerifyCodeFixAsync(source As String, fixedSource As String, includeParenthesesForMethod As Boolean) As Task
            Dim test As New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource
            }
            test.Options.Add(VisualBasicCodeStyleOptions.IncludeParenthesesForMethodInvocations, includeParenthesesForMethod)
            Await test.RunAsync()
        End Function

        Private Shared Async Function VerifyNoDiagnosticAsync(source As String, includeParenthesesForMethod As Boolean) As Task
            Dim test As New VerifyVB.Test With {.TestCode = source}
            test.Options.Add(VisualBasicCodeStyleOptions.IncludeParenthesesForMethodInvocations, includeParenthesesForMethod)
            Await test.RunAsync()
        End Function

#Region "No Diagnostic"
        <Fact>
        Public Async Function ParenthesesExistOnInvocation_OptionIsTrue_NoDiagnostic() As Task
            Dim testCode = "
Public Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
"
            Await VerifyNoDiagnosticAsync(testCode, includeParenthesesForMethod:=True)
        End Function

        <Fact>
        Public Async Function ParenthesesDoesNotExistOnInvocation_OptionIsFalse_NoDiagnostic() As Task
            Dim testCode = "
Public Module Module1
    Sub Main()
        System.Console.WriteLine
    End Sub
End Module
"
            Await VerifyNoDiagnosticAsync(testCode, includeParenthesesForMethod:=False)
        End Function

#End Region

#Region "Diagnostic"
        <Fact>
        Public Async Function ParenthesesExistOnInvocation_OptionIsFalse_Diagnostic() As Task
            Dim testCode = "
Public Module Module1
    Sub Main()
        System.Console.WriteLine[|()|]
    End Sub
End Module
"
            Dim expected = "
Public Module Module1
    Sub Main()
        System.Console.WriteLine
    End Sub
End Module
"
            Await VerifyCodeFixAsync(testCode, expected, includeParenthesesForMethod:=False)
        End Function

        <Fact>
        Public Async Function ParenthesesDoesNotExistOnInvocation_OptionIsTrue_Diagnostic() As Task
            Dim testCode = "
Public Module Module1
    Sub Main()
        [|System.Console.WriteLine|]
    End Sub
End Module
"

            Dim expected = "
Public Module Module1
    Sub Main()
        System.Console.WriteLine()
    End Sub
End Module
"
            Await VerifyCodeFixAsync(testCode, expected, includeParenthesesForMethod:=True)
        End Function
#End Region
    End Class
End Namespace
