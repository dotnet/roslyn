' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal.VisualBasicRemoveUnnecessaryByValDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryByVal.VisualBasicRemoveUnnecessaryByValCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnnecessaryByVal
    <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveByVal)>
    Public Class RemoveUnnecessaryByValTests
        Private Shared Async Function VerifyCodeFixAsync(source As String, fixedSource As String) As Task
            Await New VerifyVB.Test With
            {
                .TestCode = source,
                .FixedCode = fixedSource
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestRemoveByVal() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Sub MySub([|ByVal|] arg As String)
    End Sub
End Class
",
"Public Class Program
    Public Sub MySub(arg As String)
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function TestRemoveByValLowerCase() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Sub MySub([|byval|] arg As String)
    End Sub
End Class
",
"Public Class Program
    Public Sub MySub(arg As String)
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function TestRemoveByValMoreThanOneModifier() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Sub MySub(Optional [|ByVal|] arg As String = ""Default"")
    End Sub
End Class
",
"Public Class Program
    Public Sub MySub(Optional arg As String = ""Default"")
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function TestRemoveByValCodeHasError() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Sub MySub([|ByVal|] arg)
    End Sub
End Class
",
"Public Class Program
    Public Sub MySub(arg)
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function TestRemoveByValInConstructor() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Sub New([|ByVal|] arg As String)
    End Sub
End Class
",
"Public Class Program
    Public Sub New(arg As String)
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function TestRemoveByValInOperator() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Shared Operator +([|ByVal|] arg1 As Program, [|ByVal|] arg2 As Program) As Program
        Return New Program()
    End Operator
End Class
",
"Public Class Program
    Public Shared Operator +(arg1 As Program, arg2 As Program) As Program
        Return New Program()
    End Operator
End Class
")
        End Function

        <Fact>
        Public Async Function TestRemoveByValParameterizedProperty() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public ReadOnly Property Test([|ByVal|] v as String) As Integer
        Get
            Return 0
        End Get
    End Property
End Class
",
"Public Class Program
    Public ReadOnly Property Test(v as String) As Integer
        Get
            Return 0
        End Get
    End Property
End Class
")
        End Function

        <Fact>
        Public Async Function TestRemoveByValInDelegate() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Delegate Function CompareNumbers([|ByVal|] num1 As Integer, [|ByVal|] num2 As Integer) As Boolean
End Class
",
"Public Class Program
    Delegate Function CompareNumbers(num1 As Integer, num2 As Integer) As Boolean
End Class
")
        End Function

        <Fact>
        Public Async Function TestRemoveByValInLambdaSingleLine() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Shared Sub Main()
        Dim add1 = Function([|ByVal|] num As Integer) num + 1
        Dim print = Sub([|ByVal|] str As String) System.Console.WriteLine(str)
    End Sub
End Class
",
"Public Class Program
    Public Shared Sub Main()
        Dim add1 = Function(num As Integer) num + 1
        Dim print = Sub(str As String) System.Console.WriteLine(str)
    End Sub
End Class
")
        End Function

        <Fact>
        Public Async Function TestRemoveByValInLambdaMultiLine() As Task
            Await VerifyCodeFixAsync(
"Public Class Program
    Public Shared Sub Main()
        Dim add1 = Function([|ByVal|] num As Integer)
                       Return num + 1
                   End Function
        Dim print = Sub([|ByVal|] str As String)
                       System.Console.WriteLine(str)
                    End Sub
    End Sub
End Class
",
"Public Class Program
    Public Shared Sub Main()
        Dim add1 = Function(num As Integer)
                       Return num + 1
                   End Function
        Dim print = Sub(str As String)
                       System.Console.WriteLine(str)
                    End Sub
    End Sub
End Class
")
        End Function
    End Class
End Namespace
