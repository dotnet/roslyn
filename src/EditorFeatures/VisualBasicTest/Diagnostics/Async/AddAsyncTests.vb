' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Async

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Async
    Public Class AddAsyncTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestAwaitInSubNoModifiers() As Task
            Await TestAsync(
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Sub Test() \n [|Await Task.Delay(1)|] \n End Sub \n End Module"),
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Async Sub Test() \n Await Task.Delay(1) \n End Sub \n End Module")
                )
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestAwaitInSubWithModifiers() As Task
            Await TestAsync(
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Public Shared Sub Test() \n [|Await Task.Delay(1)|] \n End Sub \n End Module"),
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Public Shared Async Sub Test() \n Await Task.Delay(1) \n End Sub \n End Module")
                )
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestAwaitInFunctionNoModifiers() As Task
            Await TestAsync(
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Function Test() As Integer \n [|Await Task.Delay(1)|] \n Function Sub \n End Module"),
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Async Function Test() As Task(Of Integer) \n Await Task.Delay(1) \n Function Sub \n End Module")
                )
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestAwaitInFunctionWithModifiers() As Task
            Await TestAsync(
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Public Shared Function Test() As Integer \n [|Await Task.Delay(1)|] \n Function Sub \n End Module"),
                NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Public Shared Async Function Test() As Task(Of Integer) \n Await Task.Delay(1) \n Function Sub \n End Module")
                )
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestAwaitInLambdaFunction() As Task
            Dim initial =
<ModuleDeclaration>
    Sub Main(args As String())
        Dim a As Action = Sub() Console.WriteLine()
        Dim b As Func(Of Task) = Function() [|Await|] Task.Run(a)
    End Sub
</ModuleDeclaration>
            Dim expected =
<ModuleDeclaration>
    Sub Main(args As String())
        Dim a As Action = Sub() Console.WriteLine()
        Dim b As Func(Of Task) = Async Function() Await Task.Run(a)
    End Sub
</ModuleDeclaration>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestAwaitInLambdaSub() As Task
            Dim initial =
<ModuleDeclaration>
    Sub Main(args As String())
        Dim a As Action = Sub() [|Await|] Task.Run(a)
    End Sub
</ModuleDeclaration>
            Dim expected =
<ModuleDeclaration>
    Sub Main(args As String())
        Dim a As Action = Async Sub() Await Task.Run(a)
    End Sub
</ModuleDeclaration>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestAwaitInMember() As Task
            Await TestMissingAsync(NewLines("Imports System \n Imports System.Threading.Tasks \n Module Program \n Dim x =[| Await Task.Delay(3)|] \n End Module"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncMethod() As Task
            Dim initial =
<ModuleDeclaration>
    Function rtrt() As Task
        [|Await Nothing|]
    End Function
</ModuleDeclaration>
            Dim expected =
<ModuleDeclaration>
    Async Function rtrt() As Task
        Await Nothing
    End Function
</ModuleDeclaration>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncVoidMethod() As Task
            Dim initial =
<ModuleDeclaration>
    Sub rtrt()
        [|Await Nothing|]
    End Sub
</ModuleDeclaration>
            Dim expected =
<ModuleDeclaration>
    Async Sub rtrt()
        Await Nothing
    End Sub
</ModuleDeclaration>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncFunction() As Task
            Dim initial =
<ModuleDeclaration>
    Function rtrt() As Task
        [|Await Nothing|]
    End Function
</ModuleDeclaration>
            Dim expected =
<ModuleDeclaration>
    Async Function rtrt() As Task
        Await Nothing
    End Function
</ModuleDeclaration>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncFunction2() As Task
            Dim initial =
<ModuleDeclaration>
    Function rtrt() As Task(Of Integer)
        [|Await Nothing|]
    End Function
</ModuleDeclaration>
            Dim expected =
<ModuleDeclaration>
    Async Function rtrt() As Task(Of Integer)
        Await Nothing
    End Function
</ModuleDeclaration>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncFunction3() As Task
            Dim initial =
<ModuleDeclaration>
    Function rtrt() As Integer
        [|Await Nothing|]
    End Function
</ModuleDeclaration>
            Dim expected =
<ModuleDeclaration>
    Async Function rtrt() As Threading.Tasks.Task(Of Integer)
        Await Nothing
    End Function
</ModuleDeclaration>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncFunction4() As Task
            Dim initial =
<File>
Class Program
    Function rtrt() As Task
        [|Await Nothing|]
    End Function
End Class
</File>
            Dim expected =
<File>
Class Program
    Async Function rtrt() As Task
        Await Nothing
    End Function
End Class
</File>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncFunction5() As Task
            Dim initial =
<File>
Class Program
    Function rtrt() As Task(Of Integer)
        [|Await Nothing|]
    End Function
End Class
</File>
            Dim expected =
<File>
Class Program
    Async Function rtrt() As Task(Of Integer)
        Await Nothing
    End Function
End Class
</File>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncFunction6() As Task
            Dim initial =
<File>
Class Program
    Function rtrt() As Integer
        [|Await Nothing|]
    End Function
End Class
</File>
            Dim expected =
<File>
Class Program
    Async Function rtrt() As System.Threading.Tasks.Task(Of Integer)
        Await Nothing
    End Function
End Class
</File>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncFunction7() As Task
            Dim initial =
<File>
Class Program
    Function rtrt() As Program
        [|Await Nothing|]
    End Function
End Class
</File>
            Dim expected =
<File>
Class Program
    Async Function rtrt() As System.Threading.Tasks.Task(Of Program)
        Await Nothing
    End Function
End Class
</File>
            Await TestAsync(initial, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestBadAwaitInNonAsyncFunction8() As Task
            Dim initial =
<File>
Class Program
    Function rtrt() As asdf
        [|Await Nothing|]
    End Function
End Class
</File>
            Dim expected =
<File>
Class Program
    Async Function rtrt() As System.Threading.Tasks.Task(Of asdf)
        Await Nothing
    End Function
End Class
</File>
            Await TestAsync(initial, expected)
        End Function

        <WorkItem(6477, "https://github.com/dotnet/roslyn/issues/6477")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAsync)>
        Public Async Function TestNullNodeCrash() As Task
            Dim initial =
<File>
Imports System
Imports System.Threading.Tasks

Module Program
    Async Sub Main(args As String())
        [|Await|]
        Await Task.Delay(7)
    End Sub
End Module
</File>
            Await TestMissingAsync(initial)
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing,
                New VisualBasicAddAsyncCodeFixProvider())
        End Function
    End Class
End Namespace
