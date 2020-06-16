' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities.TodoComments

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.TodoComment
    <[UseExportProvider]>
    Public Class TodoCommentTests
        Inherits AbstractTodoCommentTests

        Protected Overrides Function CreateWorkspace(codeWithMarker As String) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(codeWithMarker)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Colon() As Task
            Dim code = <code>' [|TODO:test|]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Space() As Task
            Dim code = <code>' [|TODO test|]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Underscore() As Task
            Dim code = <code>' TODO_test</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Number() As Task
            Dim code = <code>' TODO1 test</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Quote() As Task
            Dim code = <code>' "TODO test"</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Middle() As Task
            Dim code = <code>' Hello TODO test</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Document() As Task
            Dim code = <code>'''        [|TODO test|]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Preprocessor1() As Task
            Dim code = <code>#If DEBUG Then ' [|TODO test|]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Preprocessor2() As Task
            Dim code = <code>#If DEBUG Then ''' [|TODO test|]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Region() As Task
            Dim code = <code>#Region ' [|TODO test      |]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_EndRegion() As Task
            Dim code = <code>#End Region        '        [|TODO test      |]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_TrailingSpan() As Task
            Dim code = <code>'        [|TODO test                   |]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_REM() As Task
            Dim code = <code>REM        [|TODO test                   |]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSingleLineTodoComment_Preprocessor_REM() As Task
            Dim code = <code>#If Debug Then    REM        [|TODO test                   |]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        Public Async Function TestSinglelineDocumentComment_Multiline() As Task
            Dim code = <code>
        ''' <summary>
        ''' [|TODO : test       |]
        ''' </summary>
        '''         [|UNDONE: test2             |]</code>

            Await TestAsync(code)
        End Function

        <Fact>
        <WorkItem(606010, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606010")>
        Public Async Function TestLeftRightSingleQuote() As Task
            Dim code = <code>
         ‘[|todo　ｆｕｌｌｗｉｄｔｈ 1|]
         ’[|todo　ｆｕｌｌｗｉｄｔｈ 2|]
        </code>

            Await TestAsync(code)
        End Function

        <Fact>
        <WorkItem(606019, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606019")>
        Public Async Function TestHalfFullTodo() As Task
            Dim code = <code>
            '[|ｔoｄo whatever|]
        </code>
            Await TestAsync(code)
        End Function

        <Fact>
        <WorkItem(627723, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627723")>
        Public Async Function TestSingleQuote_Invalid1() As Task
            Dim code = <code>
            '' todo whatever
        </code>
            Await TestAsync(code)
        End Function

        <Fact>
        <WorkItem(627723, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627723")>
        Public Async Function TestSingleQuote_Invalid2() As Task
            Dim code = <code>
            '''' todo whatever
        </code>
            Await TestAsync(code)
        End Function

        <Fact>
        <WorkItem(627723, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627723")>
        Public Async Function TestSingleQuote_Invalid3() As Task
            Dim code = <code>
            ' '' todo whatever
        </code>
            Await TestAsync(code)
        End Function

        Private Overloads Function TestAsync(codeWithMarker As XElement) As Task
            Return TestAsync(codeWithMarker.NormalizedValue())
        End Function
    End Class
End Namespace
