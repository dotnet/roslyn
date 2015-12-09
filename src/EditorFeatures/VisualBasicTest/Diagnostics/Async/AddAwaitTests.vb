' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Async
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Async
    Public Class AddAwaitTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TaskNotAwaited() As Task
            Await TestAsync(
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Async Sub MySub() \n [|Task.Delay(3)|] \n End Sub \n End Module"),
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Async Sub MySub() \n Await Task.Delay(3) \n End Sub \n End Module"),
)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
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
            Await TestAsync(initial, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
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
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
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

            Await TestAsync(initial, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
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

            Await TestAsync(initial, expected, compareTokens:=False)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestAddAwaitOnAssignment() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Integer = [|MyIntMethodAsync()|] \n End Function \n Private Function MyIntMethodAsync() As Task(Of Integer) \n Return Task.FromResult(1) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Integer = Await MyIntMethodAsync() \n End Function \n Private Function MyIntMethodAsync() As Task(Of Integer) \n Return Task.FromResult(1) \n End Function \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestAddAwaitOnAssignment2() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Long = [|MyIntMethodAsync()|] \n End Function \n Private Function MyIntMethodAsync() As Task(Of Integer) \n Return Task.FromResult(1) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Long = Await MyIntMethodAsync() \n End Function \n Private Function MyIntMethodAsync() As Task(Of Integer) \n Return Task.FromResult(1) \n End Function \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestAddAwaitOnAssignment3() As Task
            Await TestMissingAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim myInt As Long = MyInt[||]MethodAsync() \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestAddAwaitOnAssignment4() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Long = [|MyIntMethodAsync()|] \n End Function \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Async Function MyTestMethod1Async() As Task \n Dim myInt As Long = Await MyIntMethodAsync() \n End Function \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestAddAwaitOnAssignment5() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim lambda = Async Sub() \n Dim myInt As Long = [|MyIntMethodAsync()|] \n End Sub \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim lambda = Async Sub() \n Dim myInt As Long = Await MyIntMethodAsync() \n End Sub \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestAddAwaitOnAssignment6() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim lambda = Async Function() As Task \n Dim myInt As Long = [|MyIntMethodAsync()|] \n End Function \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim lambda = Async Function() As Task \n Dim myInt As Long = Await MyIntMethodAsync() \n End Function \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestAddAwaitOnAssignment7() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim myInt As Long \n Dim lambda = Async Sub() myInt = [|MyIntMethodAsync()|] \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module Program \n Sub MyTestMethod1Async() \n Dim myInt As Long \n Dim lambda = Async Sub() myInt = Await MyIntMethodAsync() \n End Sub \n Private Function MyIntMethodAsync() As Task(Of Object) \n Return Task.FromResult(New Object()) \n End Function \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestTernaryOperator() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return [|If(True, Task.FromResult(0), Task.FromResult(1))|] \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return Await If(True, Task.FromResult(0), Task.FromResult(1)) \n End Function \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestTernaryOperator2() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return [|If(Nothing, Task.FromResult(1))|] \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return Await If(Nothing, Task.FromResult(1)) \n End Function \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)>
        Public Async Function TestCastExpression() As Task
            Await TestAsync(
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return [|TryCast(Nothing, Task(Of Integer))|] \n End Function \n End Module"),
NewLines("Imports System.Threading.Tasks \n Module M \n Async Function A() As Task(Of Integer) \n Return Await TryCast(Nothing, Task(Of Integer)) \n End Function \n End Module"))
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing,
                New VisualBasicAddAwaitCodeFixProvider())
        End Function
    End Class
End Namespace
