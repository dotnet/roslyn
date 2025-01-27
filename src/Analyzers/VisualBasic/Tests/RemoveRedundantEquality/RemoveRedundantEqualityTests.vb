' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.RemoveRedundantEquality.VisualBasicRemoveRedundantEqualityDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.RemoveRedundantEquality.RemoveRedundantEqualityCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveRedundantEquality
    Public Class RemoveRedundantEqualityTests
        <Fact>
        Public Async Function TestSimpleCaseForEqualsTrue() As Task
            Dim code = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return x [|=|] True
    End Function
End Module
"
            Dim fixedCode = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return x
    End Function
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestSimpleCaseForEqualsFalse() As Task
            Await VerifyVB.VerifyCodeFixAsync("
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return x [|=|] False
    End Function
End Module
", "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return Not x
    End Function
End Module
")
        End Function

        <Fact>
        Public Async Function TestSimpleCaseForNotEqualsFalse() As Task
            Dim code = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return x [|<>|] False
    End Function
End Module
"
            Dim fixedCode = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return x
    End Function
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestSimpleCaseForNotEqualsTrue_NoDiagnostics() As Task
            Await VerifyVB.VerifyCodeFixAsync("
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return x [|<>|] True
    End Function
End Module
", "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return Not x
    End Function
End Module
")
        End Function

        <Fact>
        Public Async Function TestNullable_NoDiagnostics() As Task
            Dim code = "
Public Module Module1
    public Function M1(x As Boolean?) As Boolean
        Return x = True
    End Function
End Module
"
            Await VerifyVB.VerifyAnalyzerAsync(code)
        End Function

        <Fact>
        Public Async Function TestWhenConstant_NoDiagnostics() As Task
            Dim code = "
Public Class C
    Public Const MyTrueConstant As Boolean = True

    Public Function M1(x As Boolean) As Boolean
        Return x = MyTrueConstant
    End Function
End Class
"
            Await VerifyVB.VerifyAnalyzerAsync(code)
        End Function

        <Fact>
        Public Async Function TestOverloadedOperator_NoDiagnostics() As Task
            Dim code = "
Public Class C
    Public Shared Operator =(a As C, b As Boolean) As Boolean
        Return False
    End Operator

    Public Shared Operator <>(a As C, b As Boolean) As Boolean
        Return True
    End Operator

    Public Function M1(x As C) As Boolean
        Return x = True
    End Function
End Class
"
            Await VerifyVB.VerifyAnalyzerAsync(code)
        End Function

        <Fact>
        Public Async Function TestOnLeftHandSide() As Task
            Dim code = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return True [|=|] x
    End Function
End Module
"
            Dim fixedCode = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return x
    End Function
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestInArgument() As Task
            Dim code = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return M1(x [|=|] True)
    End Function
End Module
"
            Dim fixedCode = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return M1(x)
    End Function
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(code, fixedCode)
        End Function

        <Fact>
        Public Async Function TestFixAll() As Task
            Dim code = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return True [|=|] x
    End Function

    Public Function M2(x As Boolean) As Boolean
        Return x [|<>|] False
    End Function

    Public Function M3(x As Boolean) As Boolean
        Return x [|=|] True [|=|] True
    End Function
End Module
"
            Dim fixedCode = "
Public Module Module1
    Public Function M1(x As Boolean) As Boolean
        Return x
    End Function

    Public Function M2(x As Boolean) As Boolean
        Return x
    End Function

    Public Function M3(x As Boolean) As Boolean
        Return x
    End Function
End Module
"
            Await VerifyVB.VerifyCodeFixAsync(code, fixedCode)
        End Function
    End Class
End Namespace
