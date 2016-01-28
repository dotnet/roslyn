' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForLoopBoundIdentifier() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For x = 1 To 10 \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For x = 1 To 10 \n Next x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForLoopUnboundIdentifier() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For x = 1 To 10 \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For x = 1 To 10 \n Next x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachLoopBoundIdentifier() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For Each x In {1, 2, 3} \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For Each x In {1, 2, 3} \n Next x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachLoopUnboundIdentifier() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n Next x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachNested() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n For Each y In {1, 2, 3} \n Next [|x|] \n Next x \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n For Each y In {1, 2, 3} \n Next y \n Next x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachNestedOuter() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n For Each y In {1, 2, 3} \n Next y \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n For Each y In {1, 2, 3} \n Next y \n Next x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForLoopWithDeclarator() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For x As Integer = 1 To 10 \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n Dim y As Integer \n For x As Integer = 1 To 10 \n Next x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachLoopWithDeclarator() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n Next [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n Next x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMultipleControl1() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For Each y In {1, 2, 3} \n Next [|x|], y \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For Each y In {1, 2, 3} \n Next y, y \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMultipleControl2() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For Each y In {1, 2, 3} \n Next x, [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For Each y In {1, 2, 3} \n Next x, x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMixedNestedLoop() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, [|y|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestThreeLevels() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n For Each z As Integer In {1, 2, 3} \n Next z \n Next y, [|z|] \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n For Each z As Integer In {1, 2, 3} \n Next z \n Next y, x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestExtraVariable() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, [|z|], x \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, x, x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMethodCall() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y \n Next [|y|]() \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y \n Next x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestLongExpressions() As Task
            Await TestAsync(
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next [|x + 10 + 11|], x \n End Sub \n End Module"),
NewLines("Module M1 \n Sub Main() \n For Each x As Integer In {1, 2, 4} \n For y = 1 To 10 \n Next y, x \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestNoLoop() As Task
            Await TestMissingAsync(
NewLines("Module M1 \n Sub Main() \n Next [|y|] \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMissingNesting() As Task
            Await TestMissingAsync(
NewLines("Module M1 \n Sub Main() \n For Each x In {1, 2, 3} \n Next x, [|y|] \n End Sub \n End Module"))
        End Function
    End Class
End Namespace
