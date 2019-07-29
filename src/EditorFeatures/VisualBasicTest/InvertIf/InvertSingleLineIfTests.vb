' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InvertIf

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InvertIf
    Public Class InvertSingleLineIfTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicInvertSingleLineIfCodeRefactoringProvider()
        End Function

        Public Async Function TestFixOneAsync(initial As String, expected As String) As Task
            Await TestInRegularAndScriptAsync(CreateTreeText(initial), CreateTreeText(expected))
        End Function

        Function CreateTreeText(initial As String) As String
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAnd() As Task
            Await TestFixOneAsync(
"
        [||]If a And b Then aMethod() Else bMethod()
",
"
        If Not a Or Not b Then bMethod() Else aMethod()
")
        End Function

        <WorkItem(545700, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545700")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAddEmptyArgumentListIfNeeded() As Task
            Dim markup =
<File>
Module A
    Sub Main()
        [||]If True Then : Goo : Goo
        Else
        End If
    End Sub
    Sub Goo()
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAndAlso() As Task
            Await TestFixOneAsync(
"
        [||]If a AndAlso b Then aMethod() Else bMethod()
",
"
        If Not a OrElse Not b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestCall() As Task
            Await TestFixOneAsync(
"
        [||]If a.Goo() Then aMethod() Else bMethod()
",
"
        If Not a.Goo() Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestNotIdentifier() As Task
            Await TestFixOneAsync(
"
        [||]If Not a Then aMethod() Else bMethod()
",
"
        If a Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestTrueLiteral() As Task
            Await TestFixOneAsync(
"
        [||]If True Then aMethod() Else bMethod()
",
"
        If False Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestFalseLiteral() As Task
            Await TestFixOneAsync(
"
        [||]If False Then aMethod() Else bMethod()
",
"
        If True Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestEquals() As Task
            Await TestFixOneAsync(
"
        [||]If a = b Then aMethod() Else bMethod()
",
"
        If a <> b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestNotEquals() As Task
            Await TestFixOneAsync(
"
        [||]If a <> b Then aMethod() Else bMethod()
",
"
        If a = b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestLessThan() As Task
            Await TestFixOneAsync(
"
        [||]If a < b Then aMethod() Else bMethod()
",
"
        If a >= b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestLessThanOrEqual() As Task
            Await TestFixOneAsync(
"
        [||]If a <= b Then aMethod() Else bMethod()
",
"
        If a > b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestGreaterThan() As Task
            Await TestFixOneAsync(
"
        [||]If a > b Then aMethod() Else bMethod()
",
"
        If a <= b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestGreaterThanOrEqual() As Task
            Await TestFixOneAsync(
"
        [||]If a >= b Then aMethod() Else bMethod()
",
"
        If a < b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestIs() As Task
            Await TestFixOneAsync(
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestIsNot() As Task
            Await TestFixOneAsync(
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOr() As Task
            Await TestFixOneAsync(
"
        [||]If a Or b Then aMethod() Else bMethod()
",
"
        If Not a And Not b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOrElse() As Task
            Await TestFixOneAsync(
"
        [||]If a OrElse b Then aMethod() Else bMethod()
",
"
        If Not a AndAlso Not b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOr2() As Task
            Await TestFixOneAsync(
"
        I[||]f Not a Or Not b Then aMethod() Else bMethod()
",
"
        If a And b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOrElse2() As Task
            Await TestFixOneAsync(
"
        I[||]f Not a OrElse Not b Then aMethod() Else bMethod()
",
"
        If a AndAlso b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAnd2() As Task
            Await TestFixOneAsync(
"
        [||]If Not a And Not b Then aMethod() Else bMethod()
",
"
        If a Or b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAndAlso2() As Task
            Await TestFixOneAsync(
"
        [||]If Not a AndAlso Not b Then aMethod() Else bMethod()
",
"
        If a OrElse b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestXor() As Task
            Await TestFixOneAsync(
"
        I[||]f a Xor b Then aMethod() Else bMethod()
",
"
        If Not (a Xor b) Then bMethod() Else aMethod()
")
        End Function

        <WorkItem(545411, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545411")>
        <WpfFact(Skip:="545411"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestXor2() As Task
            Await TestFixOneAsync(
"
        I[||]f Not (a Xor b) Then aMethod() Else bMethod()
",
"
        If (a Xor b) Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestNested() As Task
            Await TestFixOneAsync(
"
        [||]If (((a = b) AndAlso (c <> d)) OrElse ((e < f) AndAlso (Not g))) Then aMethod() Else bMethod()
",
"
        If (a <> b OrElse c = d) AndAlso (e >= f OrElse g) Then bMethod() Else aMethod()
")
        End Function

        <WorkItem(529746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529746")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestEscapeKeywordsIfNeeded1() As Task
            Dim markup =
<File>
Imports System.Linq
Module Program
    Sub Main()
        [||]If True Then Dim q = From x In "" Else Console.WriteLine()
        Take()
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <WorkItem(531471, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531471")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestEscapeKeywordsIfNeeded2() As Task
            Dim markup =
<File>
Imports System.Linq
Module Program
    Sub Main()
        [||]If True Then Dim q = From x In "" Else Console.WriteLine()
        Ascending()
    End Sub
    Sub Ascending()
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <WorkItem(531471, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531471")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestEscapeKeywordsIfNeeded3() As Task
            Dim markup =
<File>
Imports System.Linq
Module Program
    Sub Main()
        [||]If True Then Dim q = From x In "" Order By x Else Console.WriteLine()
        Ascending()
    End Sub
    Sub Ascending()
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <WorkItem(531472, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531472")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestEscapeKeywordsIfNeeded4() As Task
            Dim markup =
<File>
Imports System.Linq
Module Program
    Sub Main()
        [||]If True Then Dim q = From x In "" Else Console.WriteLine()
Take:   Return
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main()
        If False Then Console.WriteLine() Else Dim q = From x In ""
[Take]:   Return
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <WorkItem(531475, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531475")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestEscapeKeywordsIfNeeded5() As Task
            Dim markup =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim a = Sub() [||]If True Then Dim q = From x In "" Else Console.WriteLine()
        Take()
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSelection() As Task
            Await TestFixOneAsync(
"
        [|If a And b Then aMethod() Else bMethod()|]
",
"
        If Not a Or Not b Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMultipleStatementsSingleLineIfStatement() As Task
            Await TestFixOneAsync(
"
        If[||] a Then aMethod() : bMethod() Else cMethod() : d()
",
"
        If Not a Then cMethod() : d() Else aMethod() : bMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestTriviaAfterSingleLineIfStatement() As Task
            Await TestFixOneAsync(
"
        [||]If a Then aMethod() Else bMethod() ' I will stay put 
",
"
        If Not a Then bMethod() Else aMethod() ' I will stay put 
")
        End Function
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestParenthesizeForLogicalExpressionPrecedence() As Task
            Await TestInRegularAndScriptAsync(
"Sub Main()
    I[||]f a AndAlso b Or c Then aMethod() Else bMethod()
End Sub
End Module",
"Sub Main()
    If (Not a OrElse Not b) And Not c Then bMethod() Else aMethod()
End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestParenthesizeComparisonOperands() As Task
            Await TestFixOneAsync(
"
        [||]If 0 <= <x/>.GetHashCode Then aMethod() Else bMethod()
",
"
        If 0 > (<x/>.GetHashCode) Then bMethod() Else aMethod()
")
        End Function

        <WorkItem(529749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529749")>
        <WorkItem(530593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530593")>
        <WpfFact(Skip:="Bug 530593"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestNestedSingleLineIfs() As Task
            Await TestInRegularAndScriptAsync(
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

        <WorkItem(529747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529747")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestTryToParenthesizeAwkwardSyntaxInsideSingleLineLambdaMethod() As Task
            Await TestMissingAsync(
"Module Program
    Sub Main()
        ' Invert If 
        Dim x = Sub() I[||]f True Then Dim y Else Console.WriteLine(), z = 1
    End Sub
End Module")
        End Function

        <WorkItem(529756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOnCoditionOfSingleLineIf() As Task
            Await TestInRegularAndScriptAsync(
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

        <WorkItem(531101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531101")>
        <WpfFact(Skip:="531101"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestImplicitLineContinuationBeforeClosingParenIsRemoved() As Task
            Dim markup =
<MethodBody>
[||]If (True OrElse True
    ) Then
Else
End If
</MethodBody>

            Dim expected =
<MethodBody>
If False AndAlso False Then
Else
End If
</MethodBody>

            Await TestAsync(markup, expected)
        End Function

        <WorkItem(530758, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530758")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestParenthesizeToKeepParseTheSame1() As Task
            Dim markup =
<File>
Module Program
    Sub Main()
        [||]If 0 &gt;= &lt;x/&gt;.GetHashCode Then Console.WriteLine(1) Else Console.WriteLine(2)
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        If 0 &lt; (&lt;x/&gt;.GetHashCode) Then Console.WriteLine(2) Else Console.WriteLine(1)
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <WorkItem(607862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/607862")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestParenthesizeToKeepParseTheSame2() As Task
            Dim markup =
<File>
Module Program
    Sub Main()
        Select Nothing
            Case Sub() [||]If True Then Dim x Else Return, Nothing
        End Select
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSingleLineIdentifier() As Task
            Await TestFixOneAsync(
"
        [||]If a Then aMethod() Else bMethod()
",
"
        If Not a Then bMethod() Else aMethod()
")
        End Function
    End Class
End Namespace
