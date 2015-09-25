' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Async
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Async
    Public Class AddAwaitTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TaskNotAwaited()
            Test(
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Async Sub MySub() \n [|Task.Delay(3)|] \n End Sub \n End Module"),
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Async Sub MySub() \n Await Task.Delay(3) \n End Sub \n End Module"),
)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TaskNotAwaited_WithLeadingTrivia()
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
            Test(initial, expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub BadAsyncReturnOperand1()
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
        [|Return Test()|]
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
            Test(initial, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub FunctionNotAwaited()
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

            Test(initial, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub FunctionNotAwaited_WithLeadingTrivia()
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

            Test(initial, expected, compareTokens:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub SubLambdaNotAwaited()
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

            Test(initial, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub FunctionLambdaNotAwaited()
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

            Test(initial, expected, compareTokens:=False)
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestAddAwaitOnAssignment()
            Test(
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Integer = [|MyIntMethodAsync()|] \n End Function \n Private Function MyIntMethodAsync() As Task(Of Integer) \n Return Task.FromResult(1) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Integer = Await MyIntMethodAsync() \n End Function \n Private Function MyIntMethodAsync() As Task(Of Integer) \n Return Task.FromResult(1) \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestAddAwaitOnAssignment2()
            Test(
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Long = [|MyIntMethodAsync()|] \n End Function \n Private Function MyIntMethodAsync() As Task(Of Integer) \n Return Task.FromResult(1) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Long = Await MyIntMethodAsync() \n End Function \n Private Function MyIntMethodAsync() As Task(Of Integer) \n Return Task.FromResult(1) \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestAddAwaitOnAssignment3()
            TestMissing(
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim myInt As Long = MyInt[||]MethodAsync() \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestAddAwaitOnAssignment4()
            Test(
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Long = [|MyIntMethodAsync()|] \n End Function \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Long = Await MyIntMethodAsync() \n End Function \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestAddAwaitOnAssignment5()
            Test(
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim lambda = Async Sub() \n Dim myInt As Long = [|MyIntMethodAsync()|] \n End Sub \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim lambda = Async Sub() \n Dim myInt As Long = Await MyIntMethodAsync() \n End Sub \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestAddAwaitOnAssignment6()
            Test(
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim lambda = Async Function() As Task \n Dim myInt As Long = [|MyIntMethodAsync()|] \n End Function \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim lambda = Async Function() As Task \n Dim myInt As Long = Await MyIntMethodAsync() \n End Function \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestAddAwaitOnAssignment7()
            Test(
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim myInt As Long \n Dim lambda = Async Sub() myInt = [|MyIntMethodAsync()|] \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim myInt As Long \n Dim lambda = Async Sub() myInt = Await MyIntMethodAsync() \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestTernaryOperator()
            Test(
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return [|If(True, Task.FromResult(0), Task.FromResult(1))|] \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return Await If(True, Task.FromResult(0), Task.FromResult(1)) \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestTernaryOperator2()
            Test(
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return [|If(Nothing, Task.FromResult(1))|] \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return Await If(Nothing, Task.FromResult(1)) \n End Function \n End Module"))
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Sub TestCastExpression()
            Test(
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return [|TryCast(Nothing, Task(Of Integer))|] \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return Await TryCast(Nothing, Task(Of Integer)) \n End Function \n End Module"))
        End Sub

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing,
                New VisualBasicAddAwaitCodeFixProvider())
        End Function
    End Class
End Namespace
