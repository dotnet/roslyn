' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ConvertForEachToFor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ConvertForEachToFor
    <Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToFor)>
    Partial Public Class ConvertForEachToForTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertForEachToForCodeRefactoringProvider()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        Public Async Function EmptyBlockBody() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For Each [||] a In array
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function EmptyBody() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For Each [||] a In array : Next
    End Sub
End Class
"
            Await TestMissingInRegularAndScriptAsync(initial)
        End Function

        <Fact>
        Public Async Function Body() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For Each [||] a In array : Console.WriteLine(a) : Next
    End Sub
End Class
"
            Await TestMissingInRegularAndScriptAsync(initial)
        End Function

        <Fact>
        Public Async Function BlockBody() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For Each [||] a In array
            Console.WriteLine(a)
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
            Console.WriteLine(a)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function Comment() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        ' comment
        For Each [||] a In array ' comment
            Console.WriteLine(a)
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        ' comment
        For {|Rename:i|} = 0 To array.Length - 1 ' comment
            Dim a = array(i)
            Console.WriteLine(a)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function Comment2() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For Each [||] a In array
            ' comment
            Console.WriteLine(a)
        Next ' comment
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
            ' comment
            Console.WriteLine(a)
        Next ' comment
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function Comment3() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For Each [||] a In array
            Console.WriteLine(a)
        Next a ' comment
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
            Console.WriteLine(a)
        Next i ' comment
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function Comment4() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a In New Integer() {1, 2, 3} ' test 
            Console.WriteLine(a)
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1 ' test 
            Dim a = array(i)
            Console.WriteLine(a)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        Public Async Function Comment7() As Task
            Dim initial = "
Class Test
    Sub Method()
        ' test
        For Each [||] a In New Integer() {1, 2, 3}
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        ' test
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        Public Async Function TestCommentsLiveBetweenForEachAndArrayDeclaration() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a ' test
            In ' test
            New Integer() {1, 2, 3}
        Next
    End Sub
End Class
"
            Dim Expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, Expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        Public Async Function CommentNotSupportedCommentsAfterLineContinuation() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a _ ' test
            In ' test
            New Integer() {1, 2, 3}
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next
    End Sub
End Class
"

            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        Public Async Function LineContinuation() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a _
            In
            New Integer() {1, 2, 3}
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        Public Async Function CollectionStatement() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a In New Integer() {1, 2, 3}
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function CollectionConflict() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = 1

        For Each [||] a In New Integer() {1, 2, 3}
            Console.WriteLine(a)
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = 1

        Dim {|Rename:array1|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array1.Length - 1
            Dim a = array1(i)
            Console.WriteLine(a)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function IndexConflict() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a In New Integer() {1, 2, 3}
            Dim i = 1
            Console.WriteLine(a)
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i1|} = 0 To array.Length - 1
            Dim a = array(i1)
            Dim i = 1
            Console.WriteLine(a)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function VariableWritten() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a In New Integer() {1, 2, 3}
            a = 1
        Next
    End Sub
End Class
"
            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            {|Warning:Dim a = array(i)|}
            a = 1
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        Public Async Function StructPropertyReadFromAndAssignedToLocal() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a In New Integer?() {1, 2, 3}
            Dim b = a.Value
        Next
    End Sub
End Class
"
            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer?() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
            Dim b = a.Value
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function StructPropertyRead() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a In New Integer?() {1, 2, 3}
            a.Value
        Next
    End Sub
End Class
"
            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer?() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
            a.Value
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function WrongCaretPosition() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each a In New Integer() {1, 2, 3}
            [||]
        Next
    End Sub
End Class
"
            Await TestMissingInRegularAndScriptAsync(initial)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/35525")>
        Public Async Function TestBefore() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
       [||] For Each a In array
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/35525")>
        Public Async Function TestAfter() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For Each a In array [||]
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/35525")>
        Public Async Function TestSelection() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        [|For Each a In array
        Next|]
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        Public Async Function Field() As Task
            Dim initial = "
Class Test
    Dim list As Integer() = New Integer() {1, 2, 3}

    Sub Method()
        For Each [||] a In list
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Dim list As Integer() = New Integer() {1, 2, 3}

    Sub Method()
        For {|Rename:i|} = 0 To list.Length - 1
            Dim a = list(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        Public Async Function [Interface]() As Task
            Dim initial = "
Imports System.Collections.Generic

Class Test
    Sub Method()
        Dim list = DirectCast(New Integer() {1, 2, 3}, IList(Of Integer))
        For [||] Each a In list
        Next
    End Sub
End Class
"

            Dim expected = "
Imports System.Collections.Generic

Class Test
    Sub Method()
        Dim list = DirectCast(New Integer() {1, 2, 3}, IList(Of Integer))
        For {|Rename:i|} = 0 To list.Count - 1
            Dim a = list(i)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function ExplicitInterface() As Task
            Dim initial = "
Imports System
Imports System.Collections
Imports System.Collections.Generic

Class Test
    Sub Method()
        Dim list = New Explicit()
        For [||] Each a In list
            Console.WriteLine(a)
        Next
    End Sub
End Class

Class Explicit
    Implements IReadOnlyList(Of Integer)

    Default Public ReadOnly Property ItemExplicit(index As Integer) As Integer Implements IReadOnlyList(Of Integer).Item
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property CountExplicit As Integer Implements IReadOnlyCollection(Of Integer).Count
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Function GetEnumeratorExplicit() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class
"

            Dim expected = "
Imports System
Imports System.Collections
Imports System.Collections.Generic

Class Test
    Sub Method()
        Dim list = New Explicit()
        Dim {|Rename:list1|} = DirectCast(list, IReadOnlyList(Of Integer))

        For {|Rename:i|} = 0 To list1.Count - 1
            Dim a = list1(i)
            Console.WriteLine(a)
        Next
    End Sub
End Class

Class Explicit
    Implements IReadOnlyList(Of Integer)

    Default Public ReadOnly Property ItemExplicit(index As Integer) As Integer Implements IReadOnlyList(Of Integer).Item
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property CountExplicit As Integer Implements IReadOnlyCollection(Of Integer).Count
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Function GetEnumeratorExplicit() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Throw New NotImplementedException()
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Throw New NotImplementedException()
    End Function
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function MultipleNext() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each a [||] In New Integer() {}
            For Each b In New Integer() {}
                Console.WriteLine(a)
        Next b, a
    End Sub
End Class"

            Await TestMissingInRegularAndScriptAsync(initial)
        End Function

        <Fact>
        Public Async Function MultipleNext2() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each a In New Integer() {}
            For Each [||] b In New Integer() {}
                Console.WriteLine(a)
        Next b, a
    End Sub
End Class"

            Await TestMissingInRegularAndScriptAsync(initial)
        End Function

        <Fact>
        Public Async Function WrongNext() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a In New Integer() {}
            Console.WriteLine(a)
        Next b
    End Sub
End Class"

            Await TestMissingInRegularAndScriptAsync(initial)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31621")>
        Public Async Function KeepNext() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a In New Integer() {1, 2, 3}
        Next a
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a = array(i)
        Next i
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function IndexConflict2() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] i In New Integer() {1, 2, 3}
            Console.WriteLine(a)
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i1|} = 0 To array.Length - 1
            Dim i = array(i1)
            Console.WriteLine(a)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function UseTypeAsUsedInForeach() As Task
            Dim initial = "
Class Test
    Sub Method()
        For Each [||] a As Integer In New Integer() {1, 2, 3}
            Console.WriteLine(a)
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim {|Rename:array|} = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
            Dim a As Integer = array(i)
            Console.WriteLine(a)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function

        <Fact>
        Public Async Function UniqueLocalName() As Task
            Dim initial = "
Imports System
Imports System.Collections.Generic

Class Test
    Sub Method()
        For Each [||] a In New List(Of Integer)()
            Console.WriteLine(a)
        Next
    End Sub
End Class
"

            Dim expected = "
Imports System
Imports System.Collections.Generic

Class Test
    Sub Method()
        Dim {|Rename:list|} = New List(Of Integer)()
        For {|Rename:i|} = 0 To list.Count - 1
            Dim a = list(i)
            Console.WriteLine(a)
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function
    End Class
End Namespace
