' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Test.Utilities.TaskList

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.TaskList
    <UseExportProvider>
    Public Class VisualBasicTaskListTests
        Inherits AbstractTaskListTests

        Protected Overrides Function CreateWorkspace(codeWithMarker As String, composition As TestComposition) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateVisualBasic(codeWithMarker, composition:=composition)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Colon(host As TestHost) As Task
            Dim code = <code>' [|TODO:test|]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Space(host As TestHost) As Task
            Dim code = <code>' [|TODO test|]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Underscore(host As TestHost) As Task
            Dim code = <code>' TODO_test</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Number(host As TestHost) As Task
            Dim code = <code>' TODO1 test</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Quote(host As TestHost) As Task
            Dim code = <code>' "TODO test"</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Middle(host As TestHost) As Task
            Dim code = <code>' Hello TODO test</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Document(host As TestHost) As Task
            Dim code = <code>'''        [|TODO test|]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Preprocessor1(host As TestHost) As Task
            Dim code = <code>#If DEBUG Then ' [|TODO test|]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Preprocessor2(host As TestHost) As Task
            Dim code = <code>#If DEBUG Then ''' [|TODO test|]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Region(host As TestHost) As Task
            Dim code = <code>#Region ' [|TODO test      |]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_EndRegion(host As TestHost) As Task
            Dim code = <code>#End Region        '        [|TODO test      |]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_TrailingSpan(host As TestHost) As Task
            Dim code = <code>'        [|TODO test                   |]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_REM(host As TestHost) As Task
            Dim code = <code>REM        [|TODO test                   |]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSingleLineTodoComment_Preprocessor_REM(host As TestHost) As Task
            Dim code = <code>#If Debug Then    REM        [|TODO test                   |]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSinglelineDocumentComment_Multiline(host As TestHost) As Task
            Dim code = <code>
        ''' <summary>
        ''' [|TODO : test       |]
        ''' </summary>
        '''         [|UNDONE: test2             |]</code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606010")>
        Public Async Function TestLeftRightSingleQuote(host As TestHost) As Task
            Dim code = <code>
         ‘[|todo　ｆｕｌｌｗｉｄｔｈ 1|]
         ’[|todo　ｆｕｌｌｗｉｄｔｈ 2|]
        </code>

            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/606019")>
        Public Async Function TestHalfFullTodo(host As TestHost) As Task
            Dim code = <code>
            '[|ｔoｄo whatever|]
        </code>
            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627723")>
        Public Async Function TestSingleQuote_Invalid1(host As TestHost) As Task
            Dim code = <code>
            '' todo whatever
        </code>
            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627723")>
        Public Async Function TestSingleQuote_Invalid2(host As TestHost) As Task
            Dim code = <code>
            '''' todo whatever
        </code>
            Await TestAsync(code, host)
        End Function

        <Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/627723")>
        Public Async Function TestSingleQuote_Invalid3(host As TestHost) As Task
            Dim code = <code>
            ' '' todo whatever
        </code>
            Await TestAsync(code, host)
        End Function

        Private Overloads Function TestAsync(codeWithMarker As XElement, host As TestHost) As Task
            Return TestAsync(codeWithMarker.NormalizedValue(), host)
        End Function
    End Class
End Namespace
