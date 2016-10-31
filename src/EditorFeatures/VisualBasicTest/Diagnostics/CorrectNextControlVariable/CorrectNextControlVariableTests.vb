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

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForLoopBoundIdentifier() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        Dim y As Integer
        For x = 1 To 10
        Next [|y|]
    End Sub
End Module",
"Module M1
    Sub Main()
        Dim y As Integer
        For x = 1 To 10
        Next x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForLoopUnboundIdentifier() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For x = 1 To 10
        Next [|y|]
    End Sub
End Module",
"Module M1
    Sub Main()
        For x = 1 To 10
        Next x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachLoopBoundIdentifier() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        Dim y As Integer
        For Each x In {1, 2, 3}
        Next [|y|]
    End Sub
End Module",
"Module M1
    Sub Main()
        Dim y As Integer
        For Each x In {1, 2, 3}
        Next x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachLoopUnboundIdentifier() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x In {1, 2, 3}
        Next [|y|]
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x In {1, 2, 3}
        Next x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachNested() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x In {1, 2, 3}
            For Each y In {1, 2, 3}
            Next [|x|]
        Next x
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x In {1, 2, 3}
            For Each y In {1, 2, 3}
            Next y
        Next x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachNestedOuter() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x In {1, 2, 3}
            For Each y In {1, 2, 3}
            Next y
        Next [|y|]
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x In {1, 2, 3}
            For Each y In {1, 2, 3}
            Next y
        Next x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForLoopWithDeclarator() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        Dim y As Integer
        For x As Integer = 1 To 10
        Next [|y|]
    End Sub
End Module",
"Module M1
    Sub Main()
        Dim y As Integer
        For x As Integer = 1 To 10
        Next x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestForEachLoopWithDeclarator() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
        Next [|y|]
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
        Next x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMultipleControl1() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For Each y In {1, 2, 3}
        Next [|x|], y
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For Each y In {1, 2, 3}
        Next y, y
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMultipleControl2() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For Each y In {1, 2, 3}
        Next x, [|y|]
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For Each y In {1, 2, 3}
        Next x, x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMixedNestedLoop() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
        Next y, [|y|]
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
        Next y, x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestThreeLevels() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
                For Each z As Integer In {1, 2, 3}
                Next z
        Next y, [|z|]
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
                For Each z As Integer In {1, 2, 3}
                Next z
        Next y, x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestExtraVariable() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
        Next y, [|z|], x
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
        Next y, x, x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMethodCall() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
            Next y
        Next [|y|]()
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
            Next y
        Next x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestLongExpressions() As Task
            Await TestAsync(
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
        Next [|x + 10 + 11|], x
    End Sub
End Module",
"Module M1
    Sub Main()
        For Each x As Integer In {1, 2, 4}
            For y = 1 To 10
        Next y, x
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestNoLoop() As Task
            Await TestMissingAsync(
"Module M1
    Sub Main()
        Next [|y|]
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsCorrectNextControlVariable)>
        Public Async Function TestMissingNesting() As Task
            Await TestMissingAsync(
"Module M1
    Sub Main()
        For Each x In {1, 2, 3}
        Next x, [|y|]
    End Sub
End Module")
        End Function
    End Class
End Namespace
