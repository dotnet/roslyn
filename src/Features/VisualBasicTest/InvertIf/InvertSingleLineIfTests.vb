' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Testing
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.InvertIf.VisualBasicInvertSingleLineIfCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InvertIf
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
    Public Class InvertSingleLineIfTests
        Private Shared Async Function TestInsideSubAsync(initial As String, expected As String) As Task
            Await TestAsync(CreateTreeText(initial), CreateTreeText(expected))
        End Function

        Private Shared Async Function TestAsync(initial As String, expected As String) As Task
            Await New VerifyVB.Test With
            {
                .TestCode = initial,
                .FixedCode = expected,
                .CompilerDiagnostics = CompilerDiagnostics.None
            }.RunAsync()
        End Function

        Public Shared Function CreateTreeText(initial As String) As String
            Return "
Module Module1
    Sub Main()
        Dim a As Boolean = True
        Dim b As Boolean = True
        Dim c As Boolean = True
        Dim d As Boolean = True

" + initial + "
    End Sub

    Private Sub aMethod()

    End Sub

    Private Sub bMethod()

    End Sub

    Private Sub cMethod()

    End Sub

    Private Sub dMethod()

    End Sub

End Module

"
        End Function

        <Fact>
        Public Async Function TestAnd() As Task
            Await TestInsideSubAsync(
"
        [||]If a And b Then aMethod() Else bMethod()
",
"
        If Not a Or Not b Then bMethod() Else aMethod()
")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545700")>
        Public Async Function TestAddEmptyArgumentListIfNeeded() As Task
            Dim markup =
"
Module A
    Sub Main()
        [||]If True Then : Goo : Goo
        Else
        End If
    End Sub
    Sub Goo()
    End Sub
End Module
"

            Await TestAsync(markup, markup)
        End Function

        <Fact>
        Public Async Function TestAndAlso() As Task
            Await TestInsideSubAsync(
"
        [||]If a AndAlso b Then aMethod() Else bMethod()
",
"
        If Not a OrElse Not b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestCall() As Task
            Await TestInsideSubAsync(
"
        [||]If a.Goo() Then aMethod() Else bMethod()
",
"
        If Not a.Goo() Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestNotIdentifier() As Task
            Await TestInsideSubAsync(
"
        [||]If Not a Then aMethod() Else bMethod()
",
"
        If a Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestTrueLiteral() As Task
            Await TestInsideSubAsync(
"
        [||]If True Then aMethod() Else bMethod()
",
"
        If False Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestFalseLiteral() As Task
            Await TestInsideSubAsync(
"
        [||]If False Then aMethod() Else bMethod()
",
"
        If True Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestEquals() As Task
            Await TestInsideSubAsync(
"
        [||]If a = b Then aMethod() Else bMethod()
",
"
        If a <> b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestNotEquals() As Task
            Await TestInsideSubAsync(
"
        [||]If a <> b Then aMethod() Else bMethod()
",
"
        If a = b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestLessThan() As Task
            Await TestInsideSubAsync(
"
        [||]If a < b Then aMethod() Else bMethod()
",
"
        If a >= b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestLessThanOrEqual() As Task
            Await TestInsideSubAsync(
"
        [||]If a <= b Then aMethod() Else bMethod()
",
"
        If a > b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestGreaterThan() As Task
            Await TestInsideSubAsync(
"
        [||]If a > b Then aMethod() Else bMethod()
",
"
        If a <= b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestGreaterThanOrEqual() As Task
            Await TestInsideSubAsync(
"
        [||]If a >= b Then aMethod() Else bMethod()
",
"
        If a < b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestIs() As Task
            Await TestInsideSubAsync(
"
        Dim myObject As New Object
        Dim thisObject = myObject

        [||]If thisObject Is myObject Then aMethod() Else bMethod()
",
"
        Dim myObject As New Object
        Dim thisObject = myObject

        If thisObject IsNot myObject Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestIsNot() As Task
            Await TestInsideSubAsync(
"
        Dim myObject As New Object
        Dim thisObject = myObject

        [||]If thisObject IsNot myObject Then aMethod() Else bMethod()
",
"
        Dim myObject As New Object
        Dim thisObject = myObject

        If thisObject Is myObject Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestOr() As Task
            Await TestInsideSubAsync(
"
        [||]If a Or b Then aMethod() Else bMethod()
",
"
        If Not a And Not b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestOrElse() As Task
            Await TestInsideSubAsync(
"
        [||]If a OrElse b Then aMethod() Else bMethod()
",
"
        If Not a AndAlso Not b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestOr2() As Task
            Await TestInsideSubAsync(
"
        I[||]f Not a Or Not b Then aMethod() Else bMethod()
",
"
        If a And b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestOrElse2() As Task
            Await TestInsideSubAsync(
"
        I[||]f Not a OrElse Not b Then aMethod() Else bMethod()
",
"
        If a AndAlso b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestAnd2() As Task
            Await TestInsideSubAsync(
"
        [||]If Not a And Not b Then aMethod() Else bMethod()
",
"
        If a Or b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestAndAlso2() As Task
            Await TestInsideSubAsync(
"
        [||]If Not a AndAlso Not b Then aMethod() Else bMethod()
",
"
        If a OrElse b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestXor() As Task
            Await TestInsideSubAsync(
"
        I[||]f a Xor b Then aMethod() Else bMethod()
",
"
        If Not (a Xor b) Then bMethod() Else aMethod()
")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545411")>
        <Fact(Skip:="545411")>
        Public Async Function TestXor2() As Task
            Await TestInsideSubAsync(
"
        I[||]f Not (a Xor b) Then aMethod() Else bMethod()
",
"
        If (a Xor b) Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestNested() As Task
            Await TestInsideSubAsync(
"
        [||]If (((a = b) AndAlso (c <> d)) OrElse ((e < f) AndAlso (Not g))) Then aMethod() Else bMethod()
",
"
        If (a <> b OrElse c = d) AndAlso (e >= f OrElse g) Then bMethod() Else aMethod()
")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529746")>
        Public Async Function TestEscapeKeywordsIfNeeded1() As Task
            Dim markup =
"
Imports System.Linq
Module Program
    Sub Main()
        [||]If True Then Dim q = From x In "" Else Console.WriteLine()
        Take()
    End Sub
    Sub Take()
    End Sub
End Module
"

            Await TestAsync(markup, markup)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531471")>
        Public Async Function TestEscapeKeywordsIfNeeded2() As Task
            Dim markup =
"
Imports System.Linq
Module Program
    Sub Main()
        [||]If True Then Dim q = From x In "" Else Console.WriteLine()
        Ascending()
    End Sub
    Sub Ascending()
    End Sub
End Module
"

            Await TestAsync(markup, markup)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531471")>
        Public Async Function TestEscapeKeywordsIfNeeded3() As Task
            Dim markup =
"
Imports System.Linq
Module Program
    Sub Main()
        [||]If True Then Dim q = From x In "" Order By x Else Console.WriteLine()
        Ascending()
    End Sub
    Sub Ascending()
    End Sub
End Module
"

            Await TestAsync(markup, markup)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531472")>
        Public Async Function TestEscapeKeywordsIfNeeded4() As Task
            Dim markup =
"
Imports System.Linq
Module Program
    Sub Main()
        [||]If True Then Dim q = From x In "" Else Console.WriteLine()
Take:   Return
    End Sub
End Module
"

            Await TestAsync(markup, markup)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531475")>
        Public Async Function TestEscapeKeywordsIfNeeded5() As Task
            Dim markup =
"
Imports System.Linq
Module Program
    Sub Main()
        Dim a = Sub() [||]If True Then Dim q = From x In "" Else Console.WriteLine()
        Take()
    End Sub
    Sub Take()
    End Sub
End Module
"

            Await TestAsync(markup, markup)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35525")>
        Public Async Function TestSelection() As Task
            Await TestInsideSubAsync(
"
        [|If a And b Then aMethod() Else bMethod()|]
",
"
        If Not a Or Not b Then bMethod() Else aMethod()
")
        End Function

        <Fact>
        Public Async Function TestMultipleStatementsSingleLineIfStatement() As Task
            Await TestInsideSubAsync(
"
        If[||] a Then aMethod() : bMethod() Else cMethod() : d()
",
"
        If Not a Then cMethod() : d() Else aMethod() : bMethod()
")
        End Function

        <Fact>
        Public Async Function TestTriviaAfterSingleLineIfStatement() As Task
            Await TestInsideSubAsync(
"
        [||]If a Then aMethod() Else bMethod() ' I will stay put 
",
"
        If Not a Then bMethod() Else aMethod() ' I will stay put 
")
        End Function
        <Fact>
        Public Async Function TestParenthesizeForLogicalExpressionPrecedence() As Task
            Await TestAsync(
"Sub Main()
    I[||]f a AndAlso b Or c Then aMethod() Else bMethod()
End Sub
End Module",
"Sub Main()
    If (Not a OrElse Not b) And Not c Then bMethod() Else aMethod()
End Sub
End Module")
        End Function

        <Fact>
        Public Async Function TestParenthesizeComparisonOperands() As Task
            Await TestInsideSubAsync(
"
        [||]If 0 <= <x/>.GetHashCode Then aMethod() Else bMethod()
",
"
        If 0 > (<x/>.GetHashCode) Then bMethod() Else aMethod()
")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529749")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530593")>
        <Fact(Skip:="Bug 530593")>
        Public Async Function TestNestedSingleLineIfs() As Task
            Await TestAsync(
"Module Program
    Sub Main()
        ' Invert the 1st If 
        I[||]f True Then Console.WriteLine(1) Else If True Then Return
    End Sub
End Module",
"Module Program
    Sub Main()
        ' Invert the 1st If 
        If False Then If True Then Return Else : Else Console.WriteLine(1)
    End Sub
End Module")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529747")>
        Public Async Function TestTryToParenthesizeAwkwardSyntaxInsideSingleLineLambdaMethod() As Task
            Dim markup =
"Module Program
    Sub Main()
        ' Invert If 
        Dim x = Sub() I[||]f True Then Dim y Else Console.WriteLine(), z = 1
    End Sub
End Module"

            Await TestAsync(markup, markup)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529756")>
        Public Async Function TestOnConditionOfSingleLineIf() As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        If T[||]rue Then Return Else Console.WriteLine(""a"")
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        If False Then Console.WriteLine(""a"") Else Return
    End Sub
End Module")
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531101")>
        <Fact(Skip:="531101")>
        Public Async Function TestImplicitLineContinuationBeforeClosingParenIsRemoved() As Task
            Dim markup =
"
[||]If (True OrElse True
    ) Then
Else
End If
"

            Dim expected =
"
If False AndAlso False Then
Else
End If
"

            Await TestInsideSubAsync(markup, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530758")>
        Public Async Function TestParenthesizeToKeepParseTheSame1() As Task
            Dim markup =
"
Module Program
    Sub Main()
        [||]If 0 >= <x/>.GetHashCode Then Console.WriteLine(1) Else Console.WriteLine(2)
    End Sub
End Module
"

            Dim expected =
"
Module Program
    Sub Main()
        If 0 < (<x/>.GetHashCode) Then Console.WriteLine(2) Else Console.WriteLine(1)
    End Sub
End Module
"

            Await TestAsync(markup, expected)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607862")>
        Public Async Function TestParenthesizeToKeepParseTheSame2() As Task
            Dim markup =
"
Module Program
    Sub Main()
        Select Nothing
            Case Sub() [||]If True Then Dim x Else Return, Nothing
        End Select
    End Sub
End Module
"

            Await TestAsync(markup, markup)
        End Function

        <Fact>
        Public Async Function TestSingleLineIdentifier() As Task
            Await TestInsideSubAsync(
"
        [||]If a Then aMethod() Else bMethod()
",
"
        If Not a Then bMethod() Else aMethod()
")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45177")>
        Public Async Function TestWithMissingTrueStatementWithinUsing() As Task
            Await TestAsync(
"Module Program
    Sub M(Disposable As IDisposable)
        Dim x = True
        Using Disposable
            [||]If Not x Then End
        End Using

        Dim y = 0
    End Sub
End Module",
"Module Program
    Sub M(Disposable As IDisposable)
        Dim x = True
        Using Disposable
            If x Then Else End
        End Using

        Dim y = 0
    End Sub
End Module")
        End Function
    End Class
End Namespace
