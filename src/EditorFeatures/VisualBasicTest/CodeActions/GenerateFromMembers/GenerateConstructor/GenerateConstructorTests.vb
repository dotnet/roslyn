' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.GenerateFromMembers.GenerateConstructor
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.GenerateFromMembers
    Public Class GenerateConstructorTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New GenerateConstructorCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestSingleField()
            Test(
NewLines("Class Program \n [|Private i As Integer|] \n End Class"),
NewLines("Class Program \n Private i As Integer \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMultipleFields()
            Test(
NewLines("Class Program \n [|Private i As Integer \n Private b As String|] \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private b As String \n Public Sub New(i As Integer, b As String) \n Me.i = i \n Me.b = b \n End Sub \n End Class"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestSecondField()
            Test(
NewLines("Class Program \n Private i As Integer \n [|Private b As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private b As String \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(b As String) \n Me.b = b \n End Sub \n End Class"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestFieldAssigningConstructor()
            Test(
NewLines("Class Program \n [|Private i As Integer \n Private b As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private b As String \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, b As String) \n Me.i = i \n Me.b = b \n End Sub \n End Class"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestMissingWithExistingConstructor()
            TestMissing(
NewLines("Class Program \n [|Private i As Integer \n Private b As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, b As String) \n Me.i = i \n Me.b = b \n End Sub \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestStruct()
            Test(
NewLines("Structure S \n [|Private i As Integer|] \n End Structure"),
NewLines("Structure S \n Private i As Integer \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Structure"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenericType()
            Test(
NewLines("Class Program ( Of T ) \n [|Private i As Integer|] \n End Class"),
NewLines("Class Program ( Of T ) \n Private i As Integer \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
index:=0)
        End Sub

        <WorkItem(541995)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestSimpleDelegatingConstructor()
            Test(
NewLines("Class Program \n [|Private i As Integer \n Private b As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private b As String \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, b As String) \n Me.New(i) \n Me.b = b \n End Sub \n End Class"),
index:=1)
        End Sub

        <WorkItem(542008)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Sub TestGenerateFromNormalProperties()
            Test(
NewLines("Class Z \n [|Public Property A As Integer \n Public Property B As String|] \n End Class"),
NewLines("Class Z \n Public Sub New(a As Integer, b As String) \n Me.A = a \n Me.B = b \n End Sub \n Public Property A As Integer \n Public Property B As String \n End Class"),
index:=0)
        End Sub
    End Class
End Namespace
