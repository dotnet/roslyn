' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Testing
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(
    Of Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.MakeMethodSynchronous.VisualBasicMakeMethodSynchronousCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.MakeMethodSynchronous
    <Trait(Traits.Feature, Traits.Features.CodeActionsMakeMethodSynchronous)>
    Public Class MakeMethodSynchronousTests
        <Fact>
        Public Async Function TestTaskReturnType() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Async Function {|BC42356:Goo|}() As Task
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Sub Goo()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestTaskOfTReturnType() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Async Function {|BC42356:Goo|}() As Task(of String)
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function Goo() As String
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestSecondModifier() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Public Async Function {|BC42356:Goo|}() As Task
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Public Sub Goo()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestFirstModifier() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Async Public Function {|BC42356:Goo|}() As Task
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Public Sub Goo()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRenameMethod() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Async Sub {|BC42356:GooAsync|}()
    End Sub
End Class",
"Imports System.Threading.Tasks

Class C
    Sub Goo()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRenameMethod1() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Async Sub {|BC42356:GooAsync|}()
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

        <Fact>
        Public Async Function TestSingleLineSubLambda() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Action(of Task) =
            Async {|BC42356:Sub|}() Return
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

        <Fact>
        Public Async Function TestSingleLineFunctionLambda() As Task
            Dim source =
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Async {|BC42356:Function|}() 1
    End Sub
End Class"
            Dim expected =
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Function() {|#0:1|}
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            ' /0/Test0.vb(7) : error BC30311: Value of type 'Integer' cannot be converted to 'Task'.
            test.FixedState.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("BC30311").WithLocation(0).WithArguments("Integer", "System.Threading.Tasks.Task"))
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function TestMultiLineSubLambda() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Action(of Task) =
            Async {|BC42356:Sub|}()
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

        <Fact>
        Public Async Function TestMultiLineFunctionLambda() As Task
            Dim source =
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Async {|BC42356:Function|}()
                Return 1
            End Function
    End Sub
End Class"
            Dim expected =
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Function()
                Return {|#0:1|}
            End Function
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            ' /0/Test0.vb(8) : error BC30311: Value of type 'Integer' cannot be converted to 'Task'.
            test.FixedState.ExpectedDiagnostics.Add(DiagnosticResult.CompilerError("BC30311").WithLocation(0).WithArguments("Integer", "System.Threading.Tasks.Task"))
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller1() As Task
            Dim source =
"Imports System.Threading.Tasks

Public Class Class1
    Async Function {|BC42356:GooAsync|}() As Task
    End Function

    Async Sub BarAsync()
        Await GooAsync()
    End Sub
End Class"
            Dim expected =
"Imports System.Threading.Tasks

Public Class Class1
    Sub Goo()
    End Sub

    Async Sub {|BC42356:BarAsync|}()
        Goo()
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            test.FixedState.MarkupHandling = MarkupMode.Allow
            test.CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller2() As Task
            Dim source =
"Imports System.Threading.Tasks

Public Class Class1
    Async Function {|BC42356:GooAsync|}() As Task
    End Function

    Async Sub BarAsync()
        Await GooAsync().ConfigureAwait(false)
    End Sub
End Class"
            Dim expected =
"Imports System.Threading.Tasks

Public Class Class1
    Sub Goo()
    End Sub

    Async Sub {|BC42356:BarAsync|}()
        Goo()
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            test.FixedState.MarkupHandling = MarkupMode.Allow
            test.CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller3() As Task
            Dim source =
"Imports System.Threading.Tasks

Public Class Class1
    Async Function {|BC42356:GooAsync|}() As Task
    End Function

    Async Sub BarAsync()
        Await Me.GooAsync()
    End Sub
End Class"
            Dim expected =
"Imports System.Threading.Tasks

Public Class Class1
    Sub Goo()
    End Sub

    Async Sub {|BC42356:BarAsync|}()
        Me.Goo()
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            test.FixedState.MarkupHandling = MarkupMode.Allow
            test.CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCaller4() As Task
            Dim source =
"Imports System.Threading.Tasks

Public Class Class1
    Async Function {|BC42356:GooAsync|}() As Task
    End Function

    Async Sub BarAsync()
        Await Me.GooAsync().ConfigureAwait(false)
    End Sub
End Class"
            Dim expected =
"Imports System.Threading.Tasks

Public Class Class1
    Sub Goo()
    End Sub

    Async Sub {|BC42356:BarAsync|}()
        Me.Goo()
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            test.FixedState.MarkupHandling = MarkupMode.Allow
            test.CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCallerNested1() As Task
            Dim source =
"Imports System.Threading.Tasks

Public Class Class1
    Async Function {|BC42356:GooAsync|}(i As Integer) As Task(Of Integer)
    End Function

    Async Sub BarAsync()
        Await GooAsync(Await GooAsync(0))
    End Sub
End Class"
            Dim expected =
"Imports System.Threading.Tasks

Public Class Class1
    Function Goo(i As Integer) As Integer
    End Function

    Async Sub {|BC42356:BarAsync|}()
        Goo(Goo(0))
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            test.FixedState.MarkupHandling = MarkupMode.Allow
            test.CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13961")>
        Public Async Function TestRemoveAwaitFromCallerNested2() As Task
            Dim source =
"Imports System.Threading.Tasks

Public Class Class1
    Async Function {|BC42356:GooAsync|}(i As Integer) As Task(Of Integer)
    End Function

    Async Sub BarAsync()
        Await Me.GooAsync(Await Me.GooAsync(0).ConfigureAwait(false)).ConfigureAwait(false)
    End Sub
End Class"
            Dim expected =
"Imports System.Threading.Tasks

Public Class Class1
    Function Goo(i As Integer) As Integer
    End Function

    Async Sub {|BC42356:BarAsync|}()
        Me.Goo(Me.Goo(0))
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            test.FixedState.MarkupHandling = MarkupMode.Allow
            test.CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne
            Await test.RunAsync()
        End Function
    End Class
End Namespace
