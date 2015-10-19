' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InvertIf

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.InvertIf
    Public Class InvertIfTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New InvertIfCodeRefactoringProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestSingleLineIdentifier()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMultiLineIdentifier()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestCall()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a.Foo() Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a.Foo() Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestNotIdentifier()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If Not a Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestTrueLiteral()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If True Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If False Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestFalseLiteral()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If False Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If True Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestEquals()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a = b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a <> b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestNotEquals()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a <> b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a = b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestLessThan()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a < b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a >= b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestLessThanOrEqual()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a <= b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a > b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestGreaterThan()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a > b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a <= b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestGreaterThanOrEqual()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a >= b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a < b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestIs()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a Is b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a IsNot b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestIsNot()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a IsNot b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Is b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestOr()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a Or b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a And Not b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestAnd()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a And b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Or Not b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestOrElse()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a OrElse b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a AndAlso Not b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestAndAlso()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a AndAlso b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a OrElse Not b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestOr2()
            Test(
NewLines("Module Program \n Sub Main() \n I[||]f Not a Or Not b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a And b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestOrElse2()
            Test(
NewLines("Module Program \n Sub Main() \n I[||]f Not a OrElse Not b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a AndAlso b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestAnd2()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If Not a And Not b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Or b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestAndAlso2()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If Not a AndAlso Not b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a OrElse b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestXor()
            Test(
NewLines("Module Program \n Sub Main() \n I[||]f a Xor b Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not (a Xor b) Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WorkItem(545411)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestXor2()
            Test(
NewLines("Module Program \n Sub Main() \n I[||]f Not (a Xor b) Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Xor b Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestNested()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If (((a = b) AndAlso (c <> d)) OrElse ((e < f) AndAlso (Not g))) Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If (a <> b OrElse c = d) AndAlso (e >= f OrElse g) Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestElseIf()
            Test(
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n [||]ElseIf b Then \n b() \n Else \n c() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n ElseIf Not b Then \n c() \n Else \n b() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestKeepElseIfKeyword()
            Test(
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n [||]ElseIf b Then \n b() \n Else \n c() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n ElseIf Not b Then \n c() \n Else \n b() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMissingOnIfElseIfElse()
            TestMissing(
NewLines("Module Program \n Sub Main() \n I[||]f a Then \n a() \n Else If b Then \n b() \n Else \n c() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMissingOnNonEmptySpan()
            TestMissing(
NewLines("Module Program \n Sub Main() \n [|If a Then \n a() \n Else \n b() \n End If|] \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestDoesNotOverlapHiddenPosition1()
            Test(
NewLines("Module Program \n Sub Main() \n #End ExternalSource \n foo() \n #ExternalSource File.vb 1 \n [||]If a Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n #End ExternalSource \n foo() \n #ExternalSource File.vb 1 \n If Not a Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestDoesNotOverlapHiddenPosition2()
            Test(
NewLines("Module Program \n Sub Main() \n #ExternalSource File.vb 1 \n [||]If a Then \n a() \n Else \n b() \n End If \n #End ExternalSource \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n #ExternalSource File.vb 1 \n If Not a Then \n b() \n Else \n a() \n End If \n #End ExternalSource \n End Sub \n End Module"))
        End Sub

        <WorkItem(529624)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMissingOnOverlapsHiddenPosition1()
            TestMissing(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n #ExternalSource File.vb 1 \n a() \n #End ExternalSource \n Else \n b() \n End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(529624)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMissingOnOverlapsHiddenPosition2()
            TestMissing(
NewLines("Module Program \n Sub Main() \n If a Then \n a() \n [||]Else If b Then \n #ExternalSource File.vb 1 \n b() \n #End ExternalSource \n Else \n c() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMissingOnOverlapsHiddenPosition3()
            TestMissing(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n #ExternalSource File.vb 1 \n Else If b Then \n b() \n #End ExternalSource \n Else \n c() \n End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(529624)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMissingOnOverlapsHiddenPosition4()
            TestMissing(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n Else \n #ExternalSource File.vb 1 \n b() \n #End ExternalSource \n End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(529624)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMissingOnOverlapsHiddenPosition5()
            TestMissing(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n #ExternalSource File.vb 1 \n a() \n Else \n b() \n #End ExternalSource \n End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(529624)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMissingOnOverlapsHiddenPosition6()
            TestMissing(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n #ExternalSource File.vb 1 \n Else \n #End ExternalSource \n b() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMissingOnNonEmptyTextSpan()
            TestMissing(
NewLines("Module Program \n Sub Main() \n [|If a Th|]en a() Else b() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMultipleStatementsSingleLineIfStatement()
            Test(
NewLines("Module Program \n Sub Main() \n If[||] a Then a() : b() Else c() : d() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then c() : d() Else a() : b() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestMultipleStatementsMultiLineIfBlock()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n foo() \n bar() \n Else \n you() \n too() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then \n you() \n too() \n Else \n foo() \n bar() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestTriviaAfterSingleLineIfStatement()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a Then a() Else b() ' I will stay put \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then b() Else a() ' I will stay put \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestTriviaAfterMultiLineIfBlock()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n Else \n b() \n End If ' I will stay put \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then \n b() \n Else \n a() \n End If ' I will stay put \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestParenthesizeForLogicalExpressionPrecedence()
            Test(
NewLines("Sub Main() \n I[||]f a AndAlso b Or c Then a() Else b() \n End Sub \n End Module"),
NewLines("Sub Main() \n If (Not a OrElse Not b) And Not c Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestKeepExplicitLineContinuationTrivia()
            Test(
NewLines("Module Program \n Sub Main() \n I[||]f a And b _ \n Or c Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If (Not a Or Not b) _ \n And Not c Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestKeepTriviaInStatementsInMultiLineIfBlock()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If a Then \n a() \n \n Else \n b() \n \n \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If Not a Then \n b() \n \n \n Else \n a() \n \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestParenthesizeComparisonOperands()
            Test(
NewLines("Module Program \n Sub Main() \n [||]If 0 <= <x/>.GetHashCode Then a() Else b() \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n If 0 > (<x/>.GetHashCode) Then b() Else a() \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestSimplifyToLengthEqualsZero()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n [||]If x.Length > 0 Then \n GreaterThanZero() \n Else \n EqualsZero() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n If x.Length = 0 Then \n EqualsZero() \n Else \n GreaterThanZero() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestSimplifyToLengthEqualsZero2()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x() As String \n [||]If x.Length > 0 Then \n GreaterThanZero() \n Else \n EqualsZero() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x() As String \n If x.Length = 0 Then \n EqualsZero() \n Else \n GreaterThanZero() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestSimplifyToLengthEqualsZero4()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x() As String \n [||]If x.Length > 0x0 Then \n GreaterThanZero() \n Else \n EqualsZero() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x() As String \n If x.Length = 0x0 Then \n EqualsZero() \n Else \n GreaterThanZero() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestSimplifyToLengthEqualsZero5()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n [||]If 0 < x.Length Then \n GreaterThanZero() \n Else \n EqualsZero() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n If 0 = x.Length Then \n EqualsZero() \n Else \n GreaterThanZero() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestDoesNotSimplifyToLengthEqualsZero()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n [||]If x.Length >= 0 Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n If x.Length < 0 Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TestDoesNotSimplifyToLengthEqualsZero2()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n [||]If x.Length > 0.0 Then \n a() \n Else \n b() \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As String \n If x.Length <= 0.0 Then \n b() \n Else \n a() \n End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(529748)>
        <WorkItem(530593)>
        <WpfFact(Skip:="Bug 530593"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub ColonAfterSingleLineIfWithEmptyElse()
            Test(
NewLines("Module Program \n Sub Main() \n ' Invert If \n I[||]f False Then Return Else : Console.WriteLine(1) \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n ' Invert If \n If True Then  Else Return \n Console.WriteLine(1) \n End Sub \n End Module"))
        End Sub

        <WorkItem(529749)>
        <WorkItem(530593)>
        <WpfFact(Skip:="Bug 530593"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub NestedSingleLineIfs()
            Test(
NewLines("Module Program \n Sub Main() \n ' Invert the 1st If \n I[||]f True Then Console.WriteLine(1) Else If True Then Return \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n ' Invert the 1st If \n If False Then If True Then Return Else : Else Console.WriteLine(1) \n End Sub \n End Module"))
        End Sub

        <WorkItem(529747)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub TryToParenthesizeAwkwardSyntaxInsideSingleLineLambda()
            Test(
NewLines("Module Program \n Sub Main() \n ' Invert If \n Dim x = Sub() I[||]f True Then Dim y Else Console.WriteLine(), z = 1 \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n ' Invert If \n Dim x = (Sub() If False Then Console.WriteLine() Else Dim y), z = 1 \n End Sub \n End Module"))
        End Sub

        <WorkItem(529756)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub OnlyOnIfOfSingleLineIf()
            TestMissing(
NewLines("Module Program \n Sub Main(args As String()) \n If True [||] Then Return Else Console.WriteLine(""a"")\n End Sub \n End Module"))
        End Sub

        <WorkItem(529756)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub OnlyOnElseIf()
            TestMissing(
NewLines("Module Program \n Sub Main(args As String()) \n If False Then \n Return \n ElseIf True [||] Then \n Console.WriteLine(""b"") \n Else \n Console.WriteLine(""a"") \n End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(529756)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub OnlyOnIfOfMultiLine()
            TestMissing(
NewLines("Module Program \n Sub Main(args As String()) \n If [||]False Then \n Return \n Else \n Console.WriteLine(""a"") \n End If \n End Sub \n End Module"))
        End Sub

        <WorkItem(531101)>
        <WpfFact(Skip:="531101"), Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub ImplicitLineContinuationBeforeClosingParenIsRemoved()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(529746)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub EscapeKeywordsIfNeeded1()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(531471)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub EscapeKeywordsIfNeeded2()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(531471)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub EscapeKeywordsIfNeeded3()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(531472)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub EscapeKeywordsIfNeeded4()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(531475)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub EscapeKeywordsIfNeeded5()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(545700)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub AddEmptyArgumentListIfNeeded()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(531474)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub DoNotRemoveTypeCharactersDuringComplexification()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(530758)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub ParenthesizeToKeepParseTheSame1()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

        <WorkItem(607862)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvertIf)>
        Public Sub ParenthesizeToKeepParseTheSame2()
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

            Test(markup, expected, compareTokens:=False)
        End Sub

    End Class
End Namespace
