' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Testing
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.RemoveAsyncModifier.VisualBasicRemoveAsyncModifierCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.RemoteAsyncModifier
    <Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)>
    Public Class RemoveAsyncModifierTests

        <Fact>
        Public Async Function Function_Task() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Async Function {|BC42356:Goo|}() As Task
        If System.DateTime.Now.Ticks > 0 Then
            Return
        End If

        System.Console.WriteLine(1)
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function {|BC42356:Goo|}() As Task
        If System.DateTime.Now.Ticks > 0 Then
            Return Task.CompletedTask
        End If

        System.Console.WriteLine(1)
        Return Task.CompletedTask
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function Function_Task_Throws() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Async Function {|BC42356:Goo|}() As Task
        System.Console.WriteLine(1)

        Throw New System.ApplicationException()
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function {|BC42356:Goo|}() As Task
        System.Console.WriteLine(1)

        Throw New System.ApplicationException()
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function Function_Task_WithLambda() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Async Function {|BC42356:Goo|}() As Task
        System.Console.WriteLine(1)

        dim f as Func(of Integer) =
            Function()
                Return 1
            End Function
    End Function
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Function {|BC42356:Goo|}() As Task
        System.Console.WriteLine(1)

        dim f as Func(of Integer) =
            Function()
                Return 1
            End Function

        Return Task.CompletedTask
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function Function_TaskOfT() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Async Function {|BC42356:Goo|}() As Task(of Integer)
        Return 1
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function {|BC42356:Goo|}() As Task(of Integer)
        Return Task.FromResult(1)
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function SingleLineFunctionLambda_Task() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Async {|BC42356:Function|}() 1
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Function() Task.FromResult(1)

    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function SingleLineFunctionLambda_TaskOfT() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task(Of Integer)) =
            Async {|BC42356:Function|}() 1
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task(Of Integer)) =
            Function() Task.FromResult(1)

    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MultiLineFunctionLambda_Task() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Async {|BC42356:Function|}()
                Console.WriteLine(1)
            End Function
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task) =
            Function()
                Console.WriteLine(1)
                Return Task.CompletedTask
            End Function
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function MultiLineFunctionLambda_TaskOfT() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task(Of Integer)) =
            Async {|BC42356:Function|}()
                Return 1
            End Function
    End Sub
End Class",
"Imports System
Imports System.Threading.Tasks

Class C
    Sub Goo()
        dim f as Func(of Task(Of Integer)) =
            Function()
                Return Task.FromResult(1)
            End Function
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function Sub_Missing() As Task
            Dim source = "
Imports System

Class C
    Async Sub {|BC42356:Goo|}()
        Console.WriteLine(1)
    End Sub
End Class"
            Dim expected = "
Imports System

Class C
    Async Sub {|#0:Goo|}()
        Console.WriteLine(1)
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            ' /0/Test0.vb(5) : warning BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
            test.FixedState.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("BC42356").WithSpan(5, 15, 5, 18))
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function MultiLineSubLambda_Task_Missing() As Task
            Dim source = "
Imports System

Class C
    Sub Goo()
        dim f as Action =
            Async {|BC42356:Sub|}()
                Console.WriteLine(1)
            End Sub
    End Sub
End Class"
            Dim expected = "
Imports System

Class C
    Sub Goo()
        dim f as Action =
            Async {|#0:Sub|}()
                Console.WriteLine(1)
            End Sub
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            ' /0/Test0.vb(7) : warning BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
            test.FixedState.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("BC42356").WithSpan(7, 19, 7, 22))
            Await test.RunAsync()
        End Function

        <Fact>
        Public Async Function SingleLineSubLambda_Task_Missing() As Task
            Dim source = "
Imports System

Class C
    Sub Goo()
        dim f as Action =
            Async {|BC42356:Sub|}() Console.WriteLine(1)
    End Sub
End Class"
            Dim expected = "
Imports System

Class C
    Sub Goo()
        dim f as Action =
            Async {|#0:Sub|}() Console.WriteLine(1)
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(expected)
            ' /0/Test0.vb(7) : warning BC42356: This async method lacks 'Await' operators and so will run synchronously. Consider using the 'Await' operator to await non-blocking API calls, or 'Await Task.Run(...)' to do CPU-bound work on a background thread.
            test.FixedState.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("BC42356").WithSpan(7, 19, 7, 22))
            Await test.RunAsync()
        End Function
    End Class
End Namespace
