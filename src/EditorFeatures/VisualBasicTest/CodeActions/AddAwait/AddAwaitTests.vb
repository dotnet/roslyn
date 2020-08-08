' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.AddAwait

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.AddAwait
    <Trait(Traits.Feature, Traits.Features.AddAwait)>
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

    End Class
End Namespace
