﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.MakeMethodSynchronous

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.MakeMethodSynchronous
    Public Class MakeMethodSynchronousTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New VisualBasicMakeMethodSynchronousCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestTaskReturnType() As Task
            Await TestAsync(
"Imports System.Threading.Tasks

Class C
    Async Function [|Foo|]() As Task
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Sub Foo()
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestTaskOfTReturnType() As Task
            Await TestAsync(
"Imports System.Threading.Tasks

Class C
    Async Function [|Foo|]() As Task(of String)
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function Foo() As String
    End Function
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestSecondModifier() As Task
            Await TestAsync(
"Imports System.Threading.Tasks

Class C
    Public Async Function [|Foo|]() As Task
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Public Sub Foo()
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestFirstModifier() As Task
            Await TestAsync(
"Imports System.Threading.Tasks

Class C
    Async Public Function [|Foo|]() As Task
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Public Sub Foo()
    End Sub
End Class",
compareTokens:=False)
        End Function



        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestRenameMethod() As Task
            Await TestAsync(
"Imports System.Threading.Tasks

Class C
    Async Sub [|FooAsync|]()
    End Sub
End Class",
"Imports System.Threading.Tasks

Class C
    Sub Foo()
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestRenameMethod1() As Task
            Await TestAsync(
"Imports System.Threading.Tasks

Class C
    Async Sub [|FooAsync|]()
    End Sub

    Sub Bar()
        FooAsync()
    End Sub
End Class",
"Imports System.Threading.Tasks

Class C
    Sub Foo()
    End Sub

    Sub Bar()
        Foo()
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestSingleLineSubLambda() As Task
            Await TestAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Foo()
        dim f as Action(of Task) =
            Async [|Sub|]() Return
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Foo()
        dim f as Action(of Task) =
            Sub() Return
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestSingleLineFunctionLambda() As Task
            Await TestAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Foo()
        dim f as Func(of Task) =
            Async [|Function|]() 1
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Foo()
        dim f as Func(of Task) =
            Function() 1
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestMultiLineSubLambda() As Task
            Await TestAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Foo()
        dim f as Action(of Task) =
            Async [|Sub|]()
                Return
            End Sub
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Foo()
        dim f as Action(of Task) =
            Sub()
                Return
            End Sub
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        Public Async Function TestMultiLineFunctionLambda() As Task
            Await TestAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Foo()
        dim f as Func(of Task) =
            Async [|Function|]()
                Return 1
            End Function
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Foo()
        dim f as Func(of Task) =
            Function()
                Return 1
            End Function
    End Sub
End Class",
compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller1() As Task
            Await TestAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|FooAsync|]() As Task
    End Function

    Async Sub BarAsync()
        Await FooAsync()
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Sub Foo()
    End Sub

    Async Sub BarAsync()
        Foo()
    End Sub
End Class", compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller2() As Task
            Await TestAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|FooAsync|]() As Task
    End Function

    Async Sub BarAsync()
        Await FooAsync().ConfigureAwait(false)
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Sub Foo()
    End Sub

    Async Sub BarAsync()
        Foo()
    End Sub
End Class", compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller3() As Task
            Await TestAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|FooAsync|]() As Task
    End Function

    Async Sub BarAsync()
        Await Me.FooAsync()
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Sub Foo()
    End Sub

    Async Sub BarAsync()
        Me.Foo()
    End Sub
End Class", compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller4() As Task
            Await TestAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|FooAsync|]() As Task
    End Function

    Async Sub BarAsync()
        Await Me.FooAsync().ConfigureAwait(false)
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Sub Foo()
    End Sub

    Async Sub BarAsync()
        Me.Foo()
    End Sub
End Class", compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCallerNested1() As Task
            Await TestAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|FooAsync|](i As Integer) As Task(Of Integer)
    End Function

    Async Sub BarAsync()
        Await FooAsync(Await FooAsync(0))
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Function Foo(i As Integer) As Integer
    End Function

    Async Sub BarAsync()
        Foo(Foo(0))
    End Sub
End Class", compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
        <WorkItem(13961, "https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCallerNested2() As Task
            Await TestAsync(
"Imports System.Threading.Tasks;

Public Class Class1
    Async Function [|FooAsync|](i As Integer) As Task(Of Integer)
    End Function

    Async Sub BarAsync()
        Await Me.FooAsync(Await Me.FooAsync(0).ConfigureAwait(false)).ConfigureAwait(false)
    End Sub
End Class",
"Imports System.Threading.Tasks;

Public Class Class1
    Function Foo(i As Integer) As Integer
    End Function

    Async Sub BarAsync()
        Me.Foo(Me.Foo(0))
    End Sub
End Class", compareTokens:=False)
        End Function
    End Class
End Namespace