' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.TodoComments
Imports Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.EditorUtilities
Imports Moq
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.TodoComment
    Public Class TodoCommentTests
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
        <WorkItem(606010)>
        Public Async Function TestLeftRightSingleQuote() As Task
            Dim code = <code>
         ‘[|todo　ｆｕｌｌｗｉｄｔｈ 1|]
         ’[|todo　ｆｕｌｌｗｉｄｔｈ 2|]
        </code>

            Await TestAsync(code)
        End Function

        <Fact>
        <WorkItem(606019)>
        Public Async Function TestHalfFullTodo() As Task
            Dim code = <code>
            '[|ｔoｄo whatever|]
        </code>
            Await TestAsync(code)
        End Function

        <Fact>
        <WorkItem(627723)>
        Public Async Function TestSingleQuote_Invalid1() As Task
            Dim code = <code>
            '' todo whatever
        </code>
            Await TestAsync(code)
        End Function

        <Fact>
        <WorkItem(627723)>
        Public Async Function TestSingleQuote_Invalid2() As Task
            Dim code = <code>
            '''' todo whatever
        </code>
            Await TestAsync(code)
        End Function

        <Fact>
        <WorkItem(627723)>
        Public Async Function TestSingleQuote_Invalid3() As Task
            Dim code = <code>
            ' '' todo whatever
        </code>
            Await TestAsync(code)
        End Function

        Private Shared Async Function TestAsync(codeWithMarker As XElement) As Tasks.Task
            Dim code As String = Nothing
            Dim list As IList(Of TextSpan) = Nothing
            MarkupTestFile.GetSpans(codeWithMarker.NormalizedValue, code, list)

            Using workspace = Await VisualBasicWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(code)
                Dim commentTokens = New TodoCommentTokens()
                Dim provider = New TodoCommentIncrementalAnalyzerProvider(commentTokens)
                Dim worker = DirectCast(provider.CreateIncrementalAnalyzer(workspace), TodoCommentIncrementalAnalyzer)

                Dim document = workspace.Documents.First()
                Dim documentId = document.Id
                Await worker.AnalyzeSyntaxAsync(workspace.CurrentSolution.GetDocument(documentId), CancellationToken.None)

                Dim todoLists = worker.GetItems_TestingOnly(documentId)

                Assert.Equal(todoLists.Count, list.Count)

                For i = 0 To todoLists.Count - 1 Step 1
                    Dim todo = todoLists(i)
                    Dim span = list(i)

                    Dim line = document.InitialTextSnapshot.GetLineFromPosition(span.Start)

                    Assert.Equal(todo.MappedLine, line.LineNumber)
                    Assert.Equal(todo.MappedColumn, span.Start - line.Start.Position)
                    Assert.Equal(todo.Message, code.Substring(span.Start, span.Length))
                Next
            End Using
        End Function
    End Class
End Namespace