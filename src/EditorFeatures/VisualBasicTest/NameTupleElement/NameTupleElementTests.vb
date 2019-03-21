' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.NameTupleElement

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.NameTupleElement

    <Trait(Traits.Feature, Traits.Features.CodeActionsNameTupleElement)>
    Public Class NameTupleElementTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicNameTupleElementCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function TestInCall_FirstElement() As Task
            Await TestInRegularAndScript1Async(
"Class C
    Sub M(x As (arg1 As Integer, arg2 As Integer))
        M(([||]1, 2))
    End Sub
End Class",
"Class C
    Sub M(x As (arg1 As Integer, arg2 As Integer))
        M((arg1:=1, 2))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestInCall_FirstComma() As Task
            Await TestInRegularAndScript1Async(
"Class C
    Sub M(x As (arg1 As Integer, arg2 As Integer))
        M((1[||], 2))
    End Sub
End Class",
"Class C
    Sub M(x As (arg1 As Integer, arg2 As Integer))
        M((arg1:=1, 2))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestInCall_SecondElement() As Task
            Await TestInRegularAndScript1Async(
"Class C
    Sub M(x As (arg1 As Integer, arg2 As Integer))
        M((1, [||]2))
    End Sub
End Class",
"Class C
    Sub M(x As (arg1 As Integer, arg2 As Integer))
        M((1, arg2:=2))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestInCall_CloseParen() As Task
            Await TestInRegularAndScript1Async(
"Class C
    Sub M(x As (arg1 As Integer, arg2 As Integer))
        M((1, 2[||]))
    End Sub
End Class",
"Class C
    Sub M(x As (arg1 As Integer, arg2 As Integer))
        M((1, arg2:=2))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestInCall_LongerTuple() As Task
            Await TestMissingAsync(
"Class C
    Sub M(x As (arg1 As Integer, arg2 As Integer))
        M((1, 2, [||]3))
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestUnnamedTuple() As Task
            Await TestMissingAsync(
"Class C
    Sub M(x As (Integer, Integer))
        M(([||]1, 2))
    End Sub
End Class")
        End Function
    End Class
End Namespace
