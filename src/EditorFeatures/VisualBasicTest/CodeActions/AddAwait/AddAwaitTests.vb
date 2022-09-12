' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.AddAwait

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.AddAwait
    <Trait(Traits.Feature, Traits.Features.AddAwait)>
    <Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
    Public Class AddAwaitTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicAddAwaitCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function Simple() As Task
            Dim markup =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = GetNumberAsync()[||]
    End Function
End Module
</File>

            Dim expected =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = Await GetNumberAsync()
    End Function
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact>
        Public Async Function SimpleWithConfigureAwait() As Task
            Dim markup =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = GetNumberAsync()[||]
    End Function
End Module
</File>

            Dim expected =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = Await GetNumberAsync().ConfigureAwait(False)
    End Function
End Module
</File>

            Await TestAsync(markup, expected, index:=1)
        End Function

        <Fact>
        Public Async Function AlreadyAwaited() As Task
            Dim markup =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = Await GetNumberAsync()[||]
    End Function
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <Fact>
        Public Async Function SimpleWithTrivia() As Task
            Dim markup =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = GetNumberAsync()[||] ' Comment
    End Function
End Module
</File>

            Dim expected =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = Await GetNumberAsync() ' Comment
    End Function
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact>
        Public Async Function SimpleWithTriviaAndConfigureAwait() As Task
            Dim markup =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = GetNumberAsync()[||] ' Comment
    End Function
End Module
</File>

            Dim expected =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = Await GetNumberAsync().ConfigureAwait(False) ' Comment
    End Function
End Module
</File>

            Await TestAsync(markup, expected, index:=1)
        End Function

        <Fact>
        Public Async Function ChainedInvocation() As Task
            Dim markup =
<File>
Imports System.Threading.Tasks
Module Program
    Async Function GetNumberAsync() As Task(Of Integer)
        Dim x = GetNumberAsync()[||].ToString()
    End Function
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <Fact>
        Public Async Function TaskNotAwaited() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Threading.Tasks
Module Program
    Async Sub MySub()
        [|Task.Delay(3)|]
    End Sub
End Module",
"Imports System
Imports System.Threading.Tasks
Module Program
    Async Sub MySub()
        Await Task.Delay(3)
    End Sub
End Module",
)
        End Function

        <Fact>
        Public Async Function TestTaskNotAwaited_WithLeadingTrivia() As Task
            Dim initial =
<File>
Imports System
Imports System.Threading.Tasks

Module Program
    Async Sub M()
        ' Useful comment
        [|Task.Delay(3)|]
    End Sub
End Module
</File>
            Dim expected =
<File>
Imports System
Imports System.Threading.Tasks

Module Program
    Async Sub M()
        ' Useful comment
        Await Task.Delay(3)
    End Sub
End Module
</File>
            Await TestAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function TestBadAsyncReturnOperand1() As Task
            Dim initial =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Async Function Test() As Task(Of Integer)
        Return 3
    End Function

    Async Function Test2() As Task(Of Integer)
        Return [|Test()|]
    End Function
End Module
</File>
            Dim expected =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Async Function Test() As Task(Of Integer)
        Return 3
    End Function

    Async Function Test2() As Task(Of Integer)
        Return Await Test()
    End Function
End Module
</File>
            Await TestAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function TestFunctionNotAwaited() As Task
            Dim initial =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Function AwaitableFunction() As Task
        Return New Task()
    End Function

    Async Sub MySub()
        [|AwaitableFunction()|]
    End Sub
End Module
</File>
            Dim expected =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Function AwaitableFunction() As Task
        Return New Task()
    End Function

    Async Sub MySub()
        Await AwaitableFunction()
    End Sub
End Module
</File>

            Await TestAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function TestFunctionNotAwaited_WithLeadingTrivia() As Task
            Dim initial =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Function AwaitableFunction() As Task
        Return New Task()
    End Function

    Async Sub MySub()

        ' Useful comment
        [|AwaitableFunction()|]
    End Sub
End Module
</File>
            Dim expected =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Function AwaitableFunction() As Task
        Return New Task()
    End Function

    Async Sub MySub()

        ' Useful comment
        Await AwaitableFunction()
    End Sub
End Module
</File>

            Await TestAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function TestSubLambdaNotAwaited() As Task
            Dim initial =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Sub MySub()
        Dim a = Async Sub() 
                    [|Task.Delay(1)|]
                End Sub
    End Sub
End Module
</File>
            Dim expected =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Sub MySub()
        Dim a = Async Sub()
                    Await Task.Delay(1)
                End Sub
    End Sub
End Module
</File>

            Await TestAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function TestFunctionLambdaNotAwaited() As Task
            Dim initial =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Sub MySub()
        Dim a = Async Function()
                    ' Useful comment
                    [|Task.Delay(1)|]
                End Function
    End Sub
End Module
</File>
            Dim expected =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Sub MySub()
        Dim a = Async Function()
                    ' Useful comment
                    Await Task.Delay(1)
                End Function
    End Sub
End Module
</File>

            Await TestAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function TestAddAwaitOnAssignment() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    Async Function MyTestMethod1Async() As Task
        Dim myInt As Integer = [|MyIntMethodAsync()|]
    End Function
    Private Function MyIntMethodAsync() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Module",
"Imports System.Threading.Tasks
Module Program
    Async Function MyTestMethod1Async() As Task
        Dim myInt As Integer = Await MyIntMethodAsync()
    End Function
    Private Function MyIntMethodAsync() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestAddAwaitOnAssignment2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    Async Function MyTestMethod1Async() As Task
        Dim myInt As Long = [|MyIntMethodAsync()|]
    End Function
    Private Function MyIntMethodAsync() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Module",
"Imports System.Threading.Tasks
Module Program
    Async Function MyTestMethod1Async() As Task
        Dim myInt As Long = Await MyIntMethodAsync()
    End Function
    Private Function MyIntMethodAsync() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestAddAwaitOnAssignment3() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    Sub MyTestMethod1Async()
        Dim myInt As Long = MyInt[||]MethodAsync()
    End Sub
    Private Function MyIntMethodAsync() As Task(Of Object)
        Return Task.FromResult(New Object())
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestAddAwaitOnAssignment4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    Async Function MyTestMethod1Async() As Task
        Dim myInt As Long = [|MyIntMethodAsync()|]
    End Function
    Private Function MyIntMethodAsync() As Task(Of Object)
        Return Task.FromResult(New Object())
    End Function
End Module",
"Imports System.Threading.Tasks
Module Program
    Async Function MyTestMethod1Async() As Task
        Dim myInt As Long = Await MyIntMethodAsync()
    End Function
    Private Function MyIntMethodAsync() As Task(Of Object)
        Return Task.FromResult(New Object())
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestAddAwaitOnAssignment5() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    Sub MyTestMethod1Async()
        Dim lambda = Async Sub()
                         Dim myInt As Long = [|MyIntMethodAsync()|]
                     End Sub
    End Sub
    Private Function MyIntMethodAsync() As Task(Of Object)
        Return Task.FromResult(New Object())
    End Function
End Module",
"Imports System.Threading.Tasks
Module Program
    Sub MyTestMethod1Async()
        Dim lambda = Async Sub()
                         Dim myInt As Long = Await MyIntMethodAsync()
                     End Sub
    End Sub
    Private Function MyIntMethodAsync() As Task(Of Object)
        Return Task.FromResult(New Object())
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestAddAwaitOnAssignment6() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    Sub MyTestMethod1Async()
        Dim lambda = Async Function() As Task
                         Dim myInt As Long = [|MyIntMethodAsync()|]
                     End Function
    End Sub
    Private Function MyIntMethodAsync() As Task(Of Object)
        Return Task.FromResult(New Object())
    End Function
End Module",
"Imports System.Threading.Tasks
Module Program
    Sub MyTestMethod1Async()
        Dim lambda = Async Function() As Task
                         Dim myInt As Long = Await MyIntMethodAsync()
                     End Function
    End Sub
    Private Function MyIntMethodAsync() As Task(Of Object)
        Return Task.FromResult(New Object())
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestAddAwaitOnAssignment7() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module Program
    Sub MyTestMethod1Async()
        Dim myInt As Long
        Dim lambda = Async Sub() myInt = [|MyIntMethodAsync()|]
    End Sub
    Private Function MyIntMethodAsync() As Task(Of Object)
        Return Task.FromResult(New Object())
    End Function
End Module",
"Imports System.Threading.Tasks
Module Program
    Sub MyTestMethod1Async()
        Dim myInt As Long
        Dim lambda = Async Sub() myInt = Await MyIntMethodAsync()
    End Sub
    Private Function MyIntMethodAsync() As Task(Of Object)
        Return Task.FromResult(New Object())
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestTernaryOperator() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module M
    Async Function A() As Task(Of Integer)
        Return [|If(True, Task.FromResult(0), Task.FromResult(1))|]
    End Function
End Module",
"Imports System.Threading.Tasks
Module M
    Async Function A() As Task(Of Integer)
        Return Await If(True, Task.FromResult(0), Task.FromResult(1))
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestTernaryOperator2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module M
    Async Function A() As Task(Of Integer)
        Return [|If(Nothing, Task.FromResult(1))|]
    End Function
End Module",
"Imports System.Threading.Tasks
Module M
    Async Function A() As Task(Of Integer)
        Return Await If(Nothing, Task.FromResult(1))
    End Function
End Module")
        End Function

        <Fact>
        Public Async Function TestCastExpression() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Threading.Tasks
Module M
    Async Function A() As Task(Of Integer)
        Return [|TryCast(Nothing, Task(Of Integer))|]
    End Function
End Module",
"Imports System.Threading.Tasks
Module M
    Async Function A() As Task(Of Integer)
        Return Await TryCast(Nothing, Task(Of Integer))
    End Function
End Module")
        End Function
    End Class
End Namespace
