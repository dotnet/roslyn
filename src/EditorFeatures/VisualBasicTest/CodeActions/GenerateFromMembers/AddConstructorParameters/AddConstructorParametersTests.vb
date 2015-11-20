' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.GenerateFromMembers.AddConstructorParameters
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.GenerateFromMembers
    Public Class AddConstructorParameterTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New AddConstructorParametersCodeRefactoringProvider()
        End Function

        <WorkItem(530592)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)>
        Public Sub TestAdd1()
            Test(
NewLines("Class Program \n [|Private i As Integer \n Private s As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private s As String \n Public Sub New(i As Integer, s As String) \n Me.i = i \n Me.s = s \n End Sub \n End Class"))
        End Sub

        <WorkItem(530592)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)>
        Public Sub TestAddOptional1()
            Test(
NewLines("Class Program \n [|Private i As Integer \n Private s As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private s As String \n Public Sub New(i As Integer, Optional s As String = Nothing) \n Me.i = i \n Me.s = s \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(530592)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)>
        Public Sub TestAddToConstructorWithMostMatchingParameters1()
            Test(
NewLines("Class Program \n [|Private i As Integer \n Private s As String \n Private b As Boolean|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, s As String) \n Me.New(i) \n Me.s = s \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private s As String \n Private b As Boolean \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, s As String, b As Boolean) \n Me.New(i) \n Me.s = s \n Me.b = b \n End Sub \n End Class"))
        End Sub

        <WorkItem(530592)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParameters)>
        Public Sub TestAddOptionalToConstructorWithMostMatchingParameters1()
            Test(
NewLines("Class Program \n [|Private i As Integer \n Private s As String \n Private b As Boolean|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, s As String) \n Me.New(i) \n Me.s = s \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private s As String \n Private b As Boolean \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, s As String, Optional b As Boolean = Nothing) \n Me.New(i) \n Me.s = s \n Me.b = b \n End Sub \n End Class"),
index:=1)
        End Sub
    End Class
End Namespace
