' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Testing
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(
    Of Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.VisualBasic.RemoveAsyncModifier.VisualBasicRemoveAsyncModifierCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.RemoteAsyncModifier
    Public Class RemoveAsyncModifierTests

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)>
        Public Async Function Function_Task() As Task
            Await VerifyVB.VerifyCodeFixAsync(
"Imports System.Threading.Tasks

Class C
    Async Function {|BC42356:Goo|}() As Task
        System.Console.WriteLine(1)
    End Function
End Class",
"Imports System.Threading.Tasks

Class C
    Function {|BC42356:Goo|}() As Task
        System.Console.WriteLine(1)
        Return Task.CompletedTask
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)>
        Public Async Function SingleLineLambda_Task() As Task
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)>
        Public Async Function SingleLineLambda_TaskOfT() As Task
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)>
        Public Async Function MultiLineLambda_Task() As Task
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)>
        Public Async Function MultiLineLambda_TaskOfT() As Task
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveAsyncModifier)>
        Public Async Function Sub_Task_Missing() As Task
            Dim source = "
Imports System

Class C
    Async Sub Goo()
        System.Console.WriteLine(1)
    End Sub
End Class"

            Dim test = New VerifyVB.Test()
            test.TestState.Sources.Add(source)
            test.FixedState.Sources.Add(source)
            test.FixedState.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("BC42356").WithLocation(0).WithArguments("Integer", "System.Threading.Tasks.Task"))
            Await test.RunAsync()
        End Function
    End Class
End Namespace
