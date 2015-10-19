' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.CorrectNextControlVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.CorrectNextControlVariable
    Public Class CorrectNextControlVariableTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing, New CorrectNextControlVariableCodeFixProvider)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestForLoopBoundIdentifier()
            Test(
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For x = 1 To 10 \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For x = 1 To 10 \n Next x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestForLoopUnboundIdentifier()
            Test(
NewLines("Module M1 \n Sub Main() \n For x = 1 To 10 \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For x = 1 To 10 \n Next x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestForEachLoopBoundIdentifier()
            Test(
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For Each x In {1, 2, 3} \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For Each x In {1, 2, 3} \n Next x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestForEachLoopUnboundIdentifier()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n Next x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestForEachNested()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n For Each y In {1, 2, 3} \n Next [|x|] \n Next x \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n For Each y In {1, 2, 3} \n Next y \n Next x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestForEachNestedOuter()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n For Each y In {1, 2, 3} \n Next y \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n For Each y In {1, 2, 3} \n Next y \n Next x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestForLoopWithDeclarator()
            Test(
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For x As Integer = 1 To 10 \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For x As Integer = 1 To 10 \n Next x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestForEachLoopWithDeclarator()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n Next x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestMultipleControl1()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For Each y In {1, 2, 3} \n Next [|x|], y \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For Each y In {1, 2, 3} \n Next y, y \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestMultipleControl2()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For Each y In {1, 2, 3} \n Next x, [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For Each y In {1, 2, 3} \n Next x, x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestMixedNestedLoop()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestThreeLevels()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n For Each z As Integer In {1, 2, 3} \n Next z \n Next y, [|z|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n For Each z As Integer In {1, 2, 3} \n Next z \n Next y, x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestExtraVariable()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, [|z|], x \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, x, x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestMethodCall()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y \n Next [|y|]() \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y \n Next x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestLongExpressions()
            Test(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next [|x + 10 + 11|], x \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, x \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestNoLoop()
            TestMissing(
NewLines("Module M1 \n Sub Main() \n Next [|y|] \n End Sub \n End Module"))
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Sub TestMissingNesting()
            TestMissing(
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n Next x, [|y|] \n End Sub \n End Module"))
        End Sub
    End Class
End Namespace
