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
    End Class
End Namespace