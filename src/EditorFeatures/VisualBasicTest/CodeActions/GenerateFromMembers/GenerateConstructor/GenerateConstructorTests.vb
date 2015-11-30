' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestSingleField() As Task
            Await TestAsync(
NewLines("Class Program \n [|Private i As Integer|] \n End Class"),
NewLines("Class Program \n Private i As Integer \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMultipleFields() As Task
            Await TestAsync(
NewLines("Class Program \n [|Private i As Integer \n Private b As String|] \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private b As String \n Public Sub New(i As Integer, b As String) \n Me.i = i \n Me.b = b \n End Sub \n End Class"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestSecondField() As Task
            Await TestAsync(
NewLines("Class Program \n Private i As Integer \n [|Private b As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private b As String \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(b As String) \n Me.b = b \n End Sub \n End Class"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestFieldAssigningConstructor() As Task
            Await TestAsync(
NewLines("Class Program \n [|Private i As Integer \n Private b As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private b As String \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, b As String) \n Me.i = i \n Me.b = b \n End Sub \n End Class"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestMissingWithExistingConstructor() As Task
            Await TestMissingAsync(
NewLines("Class Program \n [|Private i As Integer \n Private b As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, b As String) \n Me.i = i \n Me.b = b \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestStruct() As Task
            Await TestAsync(
NewLines("Structure S \n [|Private i As Integer|] \n End Structure"),
NewLines("Structure S \n Private i As Integer \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Structure"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenericType() As Task
            Await TestAsync(
NewLines("Class Program ( Of T ) \n [|Private i As Integer|] \n End Class"),
NewLines("Class Program ( Of T ) \n Private i As Integer \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
index:=0)
        End Function

        <WorkItem(541995)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestSimpleDelegatingConstructor() As Task
            Await TestAsync(
NewLines("Class Program \n [|Private i As Integer \n Private b As String|] \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n End Class"),
NewLines("Class Program \n Private i As Integer \n Private b As String \n Public Sub New(i As Integer) \n Me.i = i \n End Sub \n Public Sub New(i As Integer, b As String) \n Me.New(i) \n Me.b = b \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(542008)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)>
        Public Async Function TestGenerateFromNormalProperties() As Task
            Await TestAsync(
NewLines("Class Z \n [|Public Property A As Integer \n Public Property B As String|] \n End Class"),
NewLines("Class Z \n Public Sub New(a As Integer, b As String) \n Me.A = a \n Me.B = b \n End Sub \n Public Property A As Integer \n Public Property B As String \n End Class"),
index:=0)
        End Function
    End Class
End Namespace
