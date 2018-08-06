' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.InvertIf
    Public Class InvertIfTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicInvertIfCodeRefactoringProvider()
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
        Public Async Function TestSingleLineIdentifier() As Task
            Await TestFixOneAsync(
"
        [||]If a Then aMethod() Else bMethod()
",
"
        If Not a Then bMethod() Else aMethod()
")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMultiLineIdentifier() As Task
            Await TestFixOneAsync(
"
        [||]If a Then
            aMethod()
        Else
            bMethod()
        End If
",
"
        If Not a Then
            bMethod()
        Else
            aMethod()
        End If
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
        Public Async Function TestAnd() As Task
            Await TestFixOneAsync(
"
        [||]If a And b Then aMethod() Else bMethod()
",
"
        If Not a Or Not b Then bMethod() Else aMethod()
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestElseIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Sub Main()
        If a Then
            aMethod()
        [||]ElseIf b Then
            bMethod()
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestKeepElseIfKeyword() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        If a Then
            aMethod()
        [||]ElseIf b Then
            bMethod()
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnIfElseIfElse() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        I[||]f a Then
            aMethod()
        Else If b Then
            bMethod()
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnNonEmptySpan() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [|If a Then
            aMethod()
        Else
            bMethod()
        End If|]
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotOverlapHiddenPosition1() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
#End ExternalSource
        goo()
#ExternalSource File.vb 1 
        [||]If a Then
            aMethod()
        Else
            bMethod()
        End If
    End Sub
End Module",
"Module Program
    Sub Main()
#End ExternalSource
        goo()
#ExternalSource File.vb 1 
        If Not a Then
            bMethod()
        Else
            aMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotOverlapHiddenPosition2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
#ExternalSource File.vb 1 
        [||]If a Then
            aMethod()
        Else
            bMethod()
        End If
#End ExternalSource
    End Sub
End Module",
"Module Program
    Sub Main()
#ExternalSource File.vb 1 
        If Not a Then
            bMethod()
        Else
            aMethod()
        End If
#End ExternalSource
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
#ExternalSource File.vb 1 
            aMethod()
#End ExternalSource
        Else
            bMethod()
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        If a Then
            aMethod()
        [||]Else If b Then
#ExternalSource File.vb 1 
            bMethod()
#End ExternalSource
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition3() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()
#ExternalSource File.vb 1 
        Else If b Then
            bMethod()
#End ExternalSource
        Else
            cMethod()
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition4() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()
        Else
#ExternalSource File.vb 1 
            bMethod()
#End ExternalSource
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition5() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
#ExternalSource File.vb 1 
            aMethod()
        Else
            bMethod()
#End ExternalSource
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition6() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()
#ExternalSource File.vb 1 
        Else
#End ExternalSource
            bMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnNonEmptyTextSpan() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [|If a Th|]en aMethod() Else bMethod()
    End Sub
End Module")
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
        Public Async Function TestMultipleStatementsMultiLineIfBlock() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            goo()
            bar()
        Else
            you()
            too()
        End If
    End Sub
End Module",
"Module Program
    Sub Main()
        If Not a Then
            you()
            too()
        Else
            goo()
            bar()
        End If
    End Sub
End Module")
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
        Public Async Function TestTriviaAfterMultiLineIfBlock() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()
        Else
            bMethod()
        End If ' I will stay put 
    End Sub
End Module",
"Module Program
    Sub Main()
        If Not a Then
            bMethod()
        Else
            aMethod()
        End If ' I will stay put 
    End Sub
End Module")
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
        Public Async Function TestKeepExplicitLineContinuationTriviaMethod() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        I[||]f a And b _
        Or c Then
            aMethod()
        Else
            bMethod()
        End If
    End Sub
End Module",
"Module Program
    Sub Main()
        If (Not a Or Not b) _
        And Not c Then
            bMethod()
        Else
            aMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestKeepTriviaInStatementsInMultiLineIfBlock() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        [||]If a Then
            aMethod()

        Else
            bMethod()


        End If
    End Sub
End Module",
"Module Program
    Sub Main()
        If Not a Then
            bMethod()


        Else
            aMethod()

        End If
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As String
        [||]If x.Length > 0 Then
            GreaterThanZero()
        Else
            EqualsZero()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x As String
        If x.Length = 0 Then
            EqualsZero()
        Else
            GreaterThanZero()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x() As String
        [||]If x.Length > 0 Then
            GreaterThanZero()
        Else
            EqualsZero()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x() As String
        If x.Length = 0 Then
            EqualsZero()
        Else
            GreaterThanZero()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero4() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x() As String
        [||]If x.Length > 0x0 Then 
            GreaterThanZero()
        Else
            EqualsZero()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x() As String
        If x.Length = 0x0 Then 
            EqualsZero()
        Else
            GreaterThanZero()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero5() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As String
        [||]If 0 < x.Length Then
            GreaterThanZero()
        Else
            EqualsZero()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x As String
        If 0 = x.Length Then
            EqualsZero()
        Else
            GreaterThanZero()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotSimplifyToLengthEqualsZero() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As String
        [||]If x.Length >= 0 Then
            aMethod()
        Else
            bMethod()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x As String
        If x.Length < 0 Then
            bMethod()
        Else
            aMethod()
        End If
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotSimplifyToLengthEqualsZero2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As String
        [||]If x.Length > 0.0 Then
            aMethod()
        Else
            bMethod()
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim x As String
        If x.Length <= 0.0 Then
            bMethod()
        Else
            aMethod()
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529748")>
        <WorkItem(530593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530593")>
        <WpfFact(Skip:="Bug 530593"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestColonAfterSingleLineIfWithEmptyElse() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        ' Invert If 
        I[||]f False Then Return Else : Console.WriteLine(1)
    End Sub
End Module",
"Module Program
    Sub Main()
        ' Invert If 
        If True Then Else Return
        Console.WriteLine(1)
    End Sub
End Module")
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
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        ' Invert If 
        Dim x = Sub() I[||]f True Then Dim y Else Console.WriteLine(), z = 1
    End Sub
End Module",
"Module Program
    Sub Main()
        ' Invert If 
        Dim x = (Sub() If False Then Console.WriteLine() Else Dim y), z = 1
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

        <WorkItem(529756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOnlyOnElseIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        If False Then
            Return
        ElseIf True [||]Then
            Console.WriteLine(""b"")
        Else
            Console.WriteLine(""a"")
        End If
    End Sub
End Module")
        End Function

        <WorkItem(529756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOnConditionOfMultiLineIfStatement() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        If [||]False Then
            Return
        Else
            Console.WriteLine(""a"")
        End If
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        If [||]True Then
            Console.WriteLine(""a"")
        Else
            Return
        End If
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

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main()
        If False Then Console.WriteLine() Else Dim q = From x In ""
        [Take]()
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
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

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main()
        If False Then Console.WriteLine() Else Dim q = From x In ""
        Ascending()
    End Sub
    Sub Ascending()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
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

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main()
        If False Then Console.WriteLine() Else Dim q = From x In "" Order By x
        [Ascending]()
    End Sub
    Sub Ascending()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
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

            Await TestAsync(markup, expected)
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

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim a = Sub() If False Then Console.WriteLine() Else Dim q = From x In ""
        [Take]()
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
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

            Dim expected =
<File>
Module A
    Sub Main()
        If False Then :        Else
            Goo() : Goo
        End If
    End Sub
    Sub Goo()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <WorkItem(531474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531474")>
        <WpfFact(Skip:="531474"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoNotRemoveTypeCharactersDuringComplexification() As Task
            Dim markup =
<File>
Imports System
    Module Program
        Sub Main()
            Goo(Function(take)
                    [||]If True Then Console.WriteLine("true") Else Console.WriteLine("false")
                    take$.ToString()
                    Return Function() 1
                End Function)
        End Sub
        Sub Goo(Of T)(x As Func(Of String, T))
        End Sub
        Sub Goo(Of T)(x As Func(Of Integer, T))
        End Sub
    End Module
</File>

            Dim expected =
<File>
Imports System
    Module Program
        Sub Main()
            Goo(Function(take)
                    If False Then Console.WriteLine("false") Else Console.WriteLine("true")
                    take$.ToString()
                    Return Function() 1
                End Function)
        End Sub
        Sub Goo(Of T)(x As Func(Of String, T))
        End Sub
        Sub Goo(Of T)(x As Func(Of Integer, T))
        End Sub
    End Module
</File>

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

            Dim expected =
<File>
Module Program
    Sub Main()
        Select Nothing
            Case (Sub() If False Then Return Else Dim x), Nothing
        End Select
    End Sub
End Module

</File>

            Await TestAsync(markup, expected)
        End Function

    End Class
End Namespace
