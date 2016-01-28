' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.InvertIf
    Public Class InvertIfTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New InvertIfCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSingleLineIdentifier() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMultiLineIdentifier() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestCall() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a.Foo() Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a.Foo() Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestNotIdentifier() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If Not a Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestTrueLiteral() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If True Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If False Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestFalseLiteral() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If False Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If True Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestEquals() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a = b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a <> b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestNotEquals() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a <> b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a = b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestLessThan() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a < b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a >= b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestLessThanOrEqual() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a <= b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a > b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestGreaterThan() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a > b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a <= b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestGreaterThanOrEqual() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a >= b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a < b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestIs() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Is b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a IsNot b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestIsNot() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a IsNot b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Is b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOr() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Or b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a And Not b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAnd() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a And b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Or Not b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOrElse() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a OrElse b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a AndAlso Not b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAndAlso() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a AndAlso b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a OrElse Not b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOr2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n I[||]f Not a Or Not b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a And b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOrElse2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n I[||]f Not a OrElse Not b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a AndAlso b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAnd2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If Not a And Not b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Or b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAndAlso2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If Not a AndAlso Not b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a OrElse b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestXor() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n I[||]f a Xor b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not (a Xor b) Then b() Else a() \n End Sub \n End Module"))
        End Function

        <WorkItem(545411, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545411")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestXor2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n I[||]f Not (a Xor b) Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Xor b Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestNested() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If (((a = b) AndAlso (c <> d)) OrElse ((e < f) AndAlso (Not g))) Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If (a <> b OrElse c = d) AndAlso (e >= f OrElse g) Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestElseIf() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n [||]ElseIf b Then \n b() \n Else \n c() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n ElseIf Not b Then \n c() \n Else \n b() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestKeepElseIfKeyword() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n [||]ElseIf b Then \n b() \n Else \n c() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n ElseIf Not b Then \n c() \n Else \n b() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnIfElseIfElse() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n I[||]f a Then \n a() \n Else If b Then \n b() \n Else \n c() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnNonEmptySpan() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n [|If a Then \n a() \n Else \n b() \n End If|] \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotOverlapHiddenPosition1() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n #End ExternalSource \n foo() \n #ExternalSource File.vb 1 \n [||]If a Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n #End ExternalSource \n foo() \n #ExternalSource File.vb 1 \n If Not a Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotOverlapHiddenPosition2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n #ExternalSource File.vb 1 \n [||]If a Then \n a() \n Else \n b() \n End If \n #End ExternalSource \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n #ExternalSource File.vb 1 \n If Not a Then \n b() \n Else \n a() \n End If \n #End ExternalSource \n End Sub \n End Module"))
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition1() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n #ExternalSource File.vb 1 \n a() \n #End ExternalSource \n Else \n b() \n End If \n End Sub \n End Module"))
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition2() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n [||]Else If b Then \n #ExternalSource File.vb 1 \n b() \n #End ExternalSource \n Else \n c() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition3() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n #ExternalSource File.vb 1 \n Else If b Then \n b() \n #End ExternalSource \n Else \n c() \n End If \n End Sub \n End Module"))
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition4() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n Else \n #ExternalSource File.vb 1 \n b() \n #End ExternalSource \n End If \n End Sub \n End Module"))
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition5() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n #ExternalSource File.vb 1 \n a() \n Else \n b() \n #End ExternalSource \n End If \n End Sub \n End Module"))
        End Function

        <WorkItem(529624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529624")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnOverlapsHiddenPosition6() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n #ExternalSource File.vb 1 \n Else \n #End ExternalSource \n b() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMissingOnNonEmptyTextSpan() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main() \n [|If a Th|]en a() Else b() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMultipleStatementsSingleLineIfStatement() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n If[||] a Then a() : b() Else c() : d() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then c() : d() Else a() : b() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestMultipleStatementsMultiLineIfBlock() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n foo() \n bar() \n Else \n you() \n too() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then \n you() \n too() \n Else \n foo() \n bar() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestTriviaAfterSingleLineIfStatement() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then a() Else b() ' I will stay put \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then b() Else a() ' I will stay put \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestTriviaAfterMultiLineIfBlock() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n Else \n b() \n End If ' I will stay put \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then \n b() \n Else \n a() \n End If ' I will stay put \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestParenthesizeForLogicalExpressionPrecedence() As Task
            Await TestAsync(
NewLines("Sub Main() \n I[||]f a AndAlso b Or c Then a() Else b() \n End Sub \n End Module"),
NewLines("Sub Main() \n If (Not a OrElse Not b) And Not c Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestKeepExplicitLineContinuationTrivia() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n I[||]f a And b _ \n Or c Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If (Not a Or Not b) _ \n And Not c Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestKeepTriviaInStatementsInMultiLineIfBlock() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n \n Else \n b() \n \n \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then \n b() \n \n \n Else \n a() \n \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestParenthesizeComparisonOperands() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n [||]If 0 <= <x/>.GetHashCode Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If 0 > (<x/>.GetHashCode) Then b() Else a() \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n [||]If x.Length > 0 Then \n GreaterThanZero() \n Else \n EqualsZero() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n If x.Length = 0 Then \n EqualsZero() \n Else \n GreaterThanZero() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x() As String \n [||]If x.Length > 0 Then \n GreaterThanZero() \n Else \n EqualsZero() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x() As String \n If x.Length = 0 Then \n EqualsZero() \n Else \n GreaterThanZero() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero4() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x() As String \n [||]If x.Length > 0x0 Then \n GreaterThanZero() \n Else \n EqualsZero() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x() As String \n If x.Length = 0x0 Then \n EqualsZero() \n Else \n GreaterThanZero() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestSimplifyToLengthEqualsZero5() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n [||]If 0 < x.Length Then \n GreaterThanZero() \n Else \n EqualsZero() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n If 0 = x.Length Then \n EqualsZero() \n Else \n GreaterThanZero() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotSimplifyToLengthEqualsZero() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n [||]If x.Length >= 0 Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n If x.Length < 0 Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoesNotSimplifyToLengthEqualsZero2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n [||]If x.Length > 0.0 Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n If x.Length <= 0.0 Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Function

        <WorkItem(529748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529748")>
        <WorkItem(530593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530593")>
        <WpfFact(Skip:="Bug 530593"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestColonAfterSingleLineIfWithEmptyElse() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n ' Invert If \n I[||]f False Then Return Else : Console.WriteLine(1) \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n ' Invert If \n If True Then  Else Return \n Console.WriteLine(1) \n End Sub \n End Module"))
        End Function

        <WorkItem(529749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529749")>
        <WorkItem(530593, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530593")>
        <WpfFact(Skip:="Bug 530593"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestNestedSingleLineIfs() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n ' Invert the 1st If \n I[||]f True Then Console.WriteLine(1) Else If True Then Return \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n ' Invert the 1st If \n If False Then If True Then Return Else : Else Console.WriteLine(1) \n End Sub \n End Module"))
        End Function

        <WorkItem(529747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529747")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestTryToParenthesizeAwkwardSyntaxInsideSingleLineLambda() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n ' Invert If \n Dim x = Sub() I[||]f True Then Dim y Else Console.WriteLine(), z = 1 \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n ' Invert If \n Dim x = (Sub() If False Then Console.WriteLine() Else Dim y), z = 1 \n End Sub \n End Module"))
        End Function

        <WorkItem(529756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOnlyOnIfOfSingleLineIf() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main(args As String()) \n If True [||] Then Return Else Console.WriteLine(""a"")\n End Sub \n End Module"))
        End Function

        <WorkItem(529756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOnlyOnElseIf() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main(args As String()) \n If False Then \n Return \n ElseIf True [||] Then \n Console.WriteLine(""b"") \n Else \n Console.WriteLine(""a"") \n End If \n End Sub \n End Module"))
        End Function

        <WorkItem(529756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestOnlyOnIfOfMultiLine() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main(args As String()) \n If [||]False Then \n Return \n Else \n Console.WriteLine(""a"") \n End If \n End Sub \n End Module"))
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

            Await TestAsync(markup, expected, compareTokens:=False)
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

            Await TestAsync(markup, expected, compareTokens:=False)
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

            Await TestAsync(markup, expected, compareTokens:=False)
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

            Await TestAsync(markup, expected, compareTokens:=False)
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

            Await TestAsync(markup, expected, compareTokens:=False)
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

            Await TestAsync(markup, expected, compareTokens:=False)
        End Function

        <WorkItem(545700, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545700")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestAddEmptyArgumentListIfNeeded() As Task
            Dim markup =
<File>
Module A
    Sub Main()
        [||]If True Then : Foo : Foo
        Else
        End If
    End Sub
    Sub Foo()
    End Sub
End Module
</File>

            Dim expected =
<File>
Module A
    Sub Main()
        If False Then : Else
            Foo() : Foo
        End If
    End Sub
    Sub Foo()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected, compareTokens:=False)
        End Function

        <WorkItem(531474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531474")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Async Function TestDoNotRemoveTypeCharactersDuringComplexification() As Task
            Dim markup =
<File>
    Module Program
        Sub Main()
            Foo(Function(take)
                    [||]If True Then Console.WriteLine("true") Else Console.WriteLine("false")
                    take$.ToString()
                    Return Function() 1
                End Function)
        End Sub
        Sub Foo(Of T)(x As Func(Of String, T))
        End Sub
        Sub Foo(Of T)(x As Func(Of Integer, T))
        End Sub
    End Module
</File>

            Dim expected =
<File>
    Module Program
        Sub Main()
            Foo(Function(take)
                    If False Then Console.WriteLine("false") Else Console.WriteLine("true")
                    take$.ToString()
                    Return Function() 1
                End Function)
        End Sub
        Sub Foo(Of T)(x As Func(Of String, T))
        End Sub
        Sub Foo(Of T)(x As Func(Of Integer, T))
        End Sub
    End Module
</File>

            Await TestAsync(markup, expected, compareTokens:=False)
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

            Await TestAsync(markup, expected, compareTokens:=False)
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

            Await TestAsync(markup, expected, compareTokens:=False)
        End Function

    End Class
End Namespace
