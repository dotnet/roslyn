' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.MakeMethodSynchronous

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.MakeMethodSynchronous
    Public Class MakeMethodSynchronousTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicMakeMethodSynchronousCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestTaskReturnType() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks

Class C
    Async Function [|Goo|]() As Task
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Sub Goo()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestTaskOfTReturnType() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks

Class C
    Async Function [|Goo|]() As Task(of String)
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function Goo() As String
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestSecondModifier() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks

Class C
    Public Async Function [|Goo|]() As Task
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Public Sub Goo()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestFirstModifier() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks

Class C
    Async Public Function [|Goo|]() As Task
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Public Sub Goo()
    End Sub
End Class")
        End Function



        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestRenameMethod() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks

Class C
    Async Sub [|GooAsync|]()
    End Sub
End Class",
"Imports System.Threading.Tasks

Class C
    Sub Goo()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestRenameMethod1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks

Class C
    Async Sub [|GooAsync|]()
    End Sub

    Sub Bar()
        GooAsync()
    End Sub
End Class",
"Imports System.Threading.Tasks

Class C
    Sub Goo()
    End Sub

    Sub Bar()
        Goo()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestSingleLineSubLambda() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Action(of Task) =
            Async [|Sub|]() Return
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Action(of Task) =
            Sub() Return
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestSingleLineFunctionLambda() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Async [|Function|]() 1
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Function() 1
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestMultiLineSubLambda() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Action(of Task) =
            Async [|Sub|]()
                Return
            End Sub
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Action(of Task) =
            Sub()
                Return
            End Sub
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestMultiLineFunctionLambda() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Async [|Function|]()
                Return 1
            End Function
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Function()
                Return 1
            End Function
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|GooAsync|]() As Task
    End Function

    Async Sub BarAsync()
        Await GooAsync()
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Sub Goo()
    End Sub

    Async Sub BarAsync()
        Goo()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|GooAsync|]() As Task
    End Function

    Async Sub BarAsync()
        Await GooAsync().ConfigureAwait(false)
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Sub Goo()
    End Sub

    Async Sub BarAsync()
        Goo()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|GooAsync|]() As Task
    End Function

    Async Sub BarAsync()
        Await Me.GooAsync()
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Sub Goo()
    End Sub

    Async Sub BarAsync()
        Me.Goo()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|GooAsync|]() As Task
    End Function

    Async Sub BarAsync()
        Await Me.GooAsync().ConfigureAwait(false)
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Sub Goo()
    End Sub

    Async Sub BarAsync()
        Me.Goo()
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCallerNested1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|GooAsync|](i As Integer) As Task(Of Integer)
    End Function

    Async Sub BarAsync()
        Await GooAsync(Await GooAsync(0))
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Function Goo(i As Integer) As Integer
    End Function

    Async Sub BarAsync()
        Goo(Goo(0))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCallerNested2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|GooAsync|](i As Integer) As Task(Of Integer)
    End Function

    Async Sub BarAsync()
        Await Me.GooAsync(Await Me.GooAsync(0).ConfigureAwait(false)).ConfigureAwait(false)
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Function Goo(i As Integer) As Integer
    End Function

    Async Sub BarAsync()
        Me.Goo(Me.Goo(0))
    End Sub
End Class")
        End Function
    End Class
End Namespace
