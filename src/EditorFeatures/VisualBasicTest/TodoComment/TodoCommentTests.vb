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

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.TodoComment
    Public Class TodoCommentTests
        <WpfFact>
        Public Sub SingleLineTodoComment_Colon()
            Dim code = <code>' [|TODO:test|]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Space()
            Dim code = <code>' [|TODO test|]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Underscore()
            Dim code = <code>' TODO_test</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Number()
            Dim code = <code>' TODO1 test</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Quote()
            Dim code = <code>' "TODO test"</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Middle()
            Dim code = <code>' Hello TODO test</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Document()
            Dim code = <code>'''        [|TODO test|]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Preprocessor1()
            Dim code = <code>#If DEBUG Then ' [|TODO test|]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Preprocessor2()
            Dim code = <code>#If DEBUG Then ''' [|TODO test|]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Region()
            Dim code = <code>#Region ' [|TODO test      |]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_EndRegion()
            Dim code = <code>#End Region        '        [|TODO test      |]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_TrailingSpan()
            Dim code = <code>'        [|TODO test                   |]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_REM()
            Dim code = <code>REM        [|TODO test                   |]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SingleLineTodoComment_Preprocessor_REM()
            Dim code = <code>#If Debug Then    REM        [|TODO test                   |]</code>

            Test(code)
        End Sub

        <WpfFact>
        Public Sub SinglelineDocumentComment_Multiline()
            Dim code = <code>
        ''' <summary>
        ''' [|TODO : test       |]
        ''' </summary>
        '''         [|UNDONE: test2             |]</code>

            Test(code)
        End Sub

        <WpfFact>
        <WorkItem(606010)>
        Public Sub LeftRightSingleQuote()
            Dim code = <code>
         ‘[|todo　ｆｕｌｌｗｉｄｔｈ 1|]
         ’[|todo　ｆｕｌｌｗｉｄｔｈ 2|]
        </code>

            Test(code)
        End Sub

        <WpfFact>
        <WorkItem(606019)>
        Public Sub HalfFullTodo()
            Dim code = <code>
            '[|ｔoｄo whatever|]
        </code>
            Test(code)
        End Sub

        <WpfFact>
        <WorkItem(627723)>
        Public Sub SingleQuote_Invalid1()
            Dim code = <code>
            '' todo whatever
        </code>
            Test(code)
        End Sub

        <WpfFact>
        <WorkItem(627723)>
        Public Sub SingleQuote_Invalid2()
            Dim code = <code>
            '''' todo whatever
        </code>
            Test(code)
        End Sub

        <WpfFact>
        <WorkItem(627723)>
        Public Sub SingleQuote_Invalid3()
            Dim code = <code>
            ' '' todo whatever
        </code>
            Test(code)
        End Sub

        Private Shared Sub Test(codeWithMarker As XElement)
            Dim code As String = Nothing
            Dim list As IList(Of TextSpan) = Nothing
            MarkupTestFile.GetSpans(codeWithMarker.NormalizedValue, code, list)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code)
                Dim commentTokens = New TodoCommentTokens()
                Dim provider = New TodoCommentIncrementalAnalyzerProvider(commentTokens)
                Dim worker = DirectCast(provider.CreateIncrementalAnalyzer(workspace), TodoCommentIncrementalAnalyzer)

                Dim document = workspace.Documents.First()
                Dim documentId = document.Id
                worker.AnalyzeSyntaxAsync(workspace.CurrentSolution.GetDocument(documentId), CancellationToken.None).Wait()

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
        End Sub
    End Class
End Namespace
