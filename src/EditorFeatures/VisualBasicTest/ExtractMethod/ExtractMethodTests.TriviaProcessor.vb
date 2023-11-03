' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractMethod
    Partial Public Class ExtractMethodTests
        <[UseExportProvider]>
        <Trait(Traits.Feature, Traits.Features.ExtractMethod)>
        Public Class TriviaProcessor

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539281")>
            Public Async Function TestCommentBeforeCode() As Threading.Tasks.Task
                Dim code = <text>Class C
    Sub M()
        [|'comment
        Console.Write(10)|]
    End Sub
End Class</text>

                Dim expected = <text>Class C
    Sub M()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        'comment
        Console.Write(10)
    End Sub
End Class</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545173")>
            Public Async Function LineContinuation() As Threading.Tasks.Task
                Dim code = <text>Module Program
    Sub Main
        Dim x = [|1. _
            ToString|]
    End Sub
End Module</text>

                Dim expected = <text>Module Program
    Sub Main
        Dim x = GetX()
    End Sub

    Private Function GetX() As String
        Return 1. _
                    ToString
    End Function
End Module</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544568")>
            Public Async Function LineContinuation2() As Threading.Tasks.Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim x1 = Function(num _
                          As _
                     Integer
                          )
                     Return [|num _
                     +
                     1|]
                 End Function
    End Sub
End Module</text>

                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim x1 = Function(num _
                          As _
                     Integer
                          )
                     Return NewMethod(num)
                 End Function
    End Sub

    Private Function NewMethod(num As Integer) As Integer
        Return num _
                             +
                             1
    End Function
End Module</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529797")>
            Public Async Function ImplicitLineContinuation() As Threading.Tasks.Task
                Dim code = <text>Imports System.Linq
Module A
    Sub Main()
        Dim q = [|From x In "" Distinct|] ' Extract Method
        .ToString()
    End Sub
End Module</text>

                Dim expected = <text>Imports System.Linq
Module A
    Sub Main()
        Dim q = NewMethod() ' Extract Method
        .ToString()
    End Sub

    Private Function NewMethod() As System.Collections.Generic.IEnumerable(Of Char)
        Return From x In "" Distinct
    End Function
End Module</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529797")>
            Public Async Function ImplicitLineContinuation2() As Threading.Tasks.Task
                Dim code = <text>Imports System.Linq
Module A
    Sub Main()
        Dim q = [|From x In "" Distinct|]
        .ToString()
    End Sub
End Module</text>

                Dim expected = <text>Imports System.Linq
Module A
    Sub Main()
        Dim q = NewMethod()
        .ToString()
    End Sub

    Private Function NewMethod() As System.Collections.Generic.IEnumerable(Of Char)
        Return From x In "" Distinct
    End Function
End Module</text>

                Await TestExtractMethodAsync(code, expected)
            End Function
        End Class
    End Class
End Namespace
