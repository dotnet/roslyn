' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching
Imports Microsoft.CodeAnalysis.Editor.Shared.Tagging
Imports Microsoft.CodeAnalysis.Editor.Tagging
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Tagging
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.BraceMatching

    Public Class BraceHighlightingTests

        Private Function Enumerable(Of t)(ParamArray array() As t) As IEnumerable(Of t)
            Return array
        End Function

        Private Function ProduceTags(workspace As TestWorkspace, buffer As ITextBuffer, position As Integer) As IEnumerable(Of ITagSpan(Of BraceHighlightTag))
            Dim producer = New BraceHighlightingViewTaggerProvider(
                workspace.GetService(Of IBraceMatchingService),
                workspace.GetService(Of IForegroundNotificationService),
                AggregateAsynchronousOperationListener.EmptyListeners)

            Dim doc = buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().FirstOrDefault()
            Dim context = New TaggerContext(Of BraceHighlightTag)(
                doc, buffer.CurrentSnapshot, New SnapshotPoint(buffer.CurrentSnapshot, position))
            producer.ProduceTagsAsync_ForTestingPurposesOnly(context).Wait()
            Return context.tagSpans
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)>
        Public Async Function TestParens() As Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromLinesAsync("Module Module1",
                             "    Function Foo(x As Integer) As Integer",
                             "    End Function",
                             "End Module")
                Dim buffer = workspace.Documents.First().GetTextBuffer()

                ' Before open parens
                Dim result = ProduceTags(workspace, buffer, 31)
                Assert.True(result.IsEmpty())

                ' At open parens
                result = ProduceTags(workspace, buffer, 32)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(32, 33), Span.FromBounds(45, 46))))

                ' After open parens
                result = ProduceTags(workspace, buffer, 33)
                Assert.True(result.IsEmpty())

                ' Before close parens
                result = ProduceTags(workspace, buffer, 44)
                Assert.True(result.IsEmpty())

                ' At close parens
                result = ProduceTags(workspace, buffer, 45)
                Assert.True(result.IsEmpty())

                ' After close parens
                result = ProduceTags(workspace, buffer, 46)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(32, 33), Span.FromBounds(45, 46))))
            End Using
        End Function


        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)>
        Public Async Function TestNestedTouchingItems() As Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromLinesAsync(
                "Module Module1",
                "    <SomeAttr(New With {.name = ""test""})>  ",
                "    Sub Foo()",
                "    End Sub",
                "End Module")
                Dim buffer = workspace.Documents.First().GetTextBuffer()

                ' pos 0 on second line is 16

                ' Before <
                Dim result = ProduceTags(workspace, buffer, 19)
                Assert.True(result.IsEmpty)

                ' On <
                result = ProduceTags(workspace, buffer, 20)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(20, 21), Span.FromBounds(56, 57))))

                ' After <
                result = ProduceTags(workspace, buffer, 21)
                Assert.True(result.IsEmpty)

                ' Before (
                result = ProduceTags(workspace, buffer, 28)
                Assert.True(result.IsEmpty)

                ' On (
                result = ProduceTags(workspace, buffer, 29)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(29, 30), Span.FromBounds(55, 56))))

                ' After (
                result = ProduceTags(workspace, buffer, 30)
                Assert.True(result.IsEmpty)

                ' Before {
                result = ProduceTags(workspace, buffer, 38)
                Assert.True(result.IsEmpty)

                ' On {
                result = ProduceTags(workspace, buffer, 39)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(39, 40), Span.FromBounds(54, 55))))

                ' After {
                result = ProduceTags(workspace, buffer, 40)
                Assert.True(result.IsEmpty)

                ' x is any character, | is the cursor in the following comments 
                '|"})>
                result = ProduceTags(workspace, buffer, 53)
                Assert.True(result.IsEmpty)

                ' "|})>
                result = ProduceTags(workspace, buffer, 54)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(48, 49), Span.FromBounds(53, 54))))

                ' }|)>
                result = ProduceTags(workspace, buffer, 55)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(39, 40), Span.FromBounds(54, 55))))

                ' })|>
                result = ProduceTags(workspace, buffer, 56)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(29, 30), Span.FromBounds(55, 56))))

                ' })>|
                result = ProduceTags(workspace, buffer, 57)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(20, 21), Span.FromBounds(56, 57))))

                ' })>x|
                result = ProduceTags(workspace, buffer, 58)
                Assert.True(result.IsEmpty)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)>
        Public Async Function TestUnnestedTouchingItems() As Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromLinesAsync("Module Module1",
                     "    Dim arr()() As Integer",
                     "End Module")
                Dim buffer = workspace.Documents.First().GetTextBuffer()

                ' x is any character, | is the cursor in the following comments
                ' |x()()
                Dim result = ProduceTags(workspace, buffer, 26)
                Assert.True(result.IsEmpty)

                ' |()()
                result = ProduceTags(workspace, buffer, 27)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(27, 28), Span.FromBounds(28, 29))))

                ' (|)()
                result = ProduceTags(workspace, buffer, 28)
                Assert.True(result.IsEmpty)

                ' ()|()
                result = ProduceTags(workspace, buffer, 29)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(27, 28), Span.FromBounds(28, 29),
                                                                                          Span.FromBounds(29, 30), Span.FromBounds(30, 31))))

                ' ()(|)
                result = ProduceTags(workspace, buffer, 30)
                Assert.True(result.IsEmpty)

                ' ()()|
                result = ProduceTags(workspace, buffer, 31)
                Assert.True(result.Select(Function(ts) ts.Span.Span).SetEquals(Enumerable(Span.FromBounds(29, 30), Span.FromBounds(30, 31))))

                ' ()()x|
                result = ProduceTags(workspace, buffer, 32)
                Assert.True(result.IsEmpty)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.BraceHighlighting)>
        Public Async Function TestAngles() As Tasks.Task
            Using workspace = Await VisualBasicWorkspaceFactory.CreateWorkspaceFromLinesAsync("Module Module1",
                     "    <Attribute()>",
                     "    Sub Foo()",
                     "        Dim x = 2 > 3",
                     "        Dim y = 4 > 5",
                     "        Dim z = <element> </element>",
                     "    End Sub",
                     "End Module")
                Dim buffer = workspace.Documents.First().GetTextBuffer()

                Dim line4start = buffer.CurrentSnapshot.GetLineFromLineNumber(3).Start.Position
                Dim line5start = buffer.CurrentSnapshot.GetLineFromLineNumber(4).Start.Position
                Dim line6start = buffer.CurrentSnapshot.GetLineFromLineNumber(5).Start.Position

                ' 2| > 3
                Dim result = ProduceTags(workspace, buffer, 15 + line4start)
                Assert.True(result.IsEmpty)

                ' 2 |> 3
                result = ProduceTags(workspace, buffer, 16 + line4start)
                Assert.True(result.IsEmpty)

                ' 2 >| 3
                result = ProduceTags(workspace, buffer, 17 + line4start)
                Assert.True(result.IsEmpty)

                ' 2 > |3
                result = ProduceTags(workspace, buffer, 18 + line4start)
                Assert.True(result.IsEmpty)

                ' 4| > 5
                result = ProduceTags(workspace, buffer, 15 + line5start)
                Assert.True(result.IsEmpty)

                ' 4 |> 5
                result = ProduceTags(workspace, buffer, 16 + line5start)
                Assert.True(result.IsEmpty)

                ' 4 >| 5
                result = ProduceTags(workspace, buffer, 17 + line5start)
                Assert.True(result.IsEmpty)

                ' 4 > |5
                result = ProduceTags(workspace, buffer, 18 + line5start)
                Assert.True(result.IsEmpty)

                ' |<element> </element>
                result = ProduceTags(workspace, buffer, 16 + line6start)
                Assert.True(result.IsEmpty)

                ' <element>| </element>
                result = ProduceTags(workspace, buffer, 25 + line6start)
                Assert.True(result.IsEmpty)

                ' <element> |</element>
                result = ProduceTags(workspace, buffer, 26 + line6start)
                Assert.True(result.IsEmpty)

                ' <element> </element>|
                result = ProduceTags(workspace, buffer, 36 + line6start)
                Assert.True(result.IsEmpty)
            End Using
        End Function
    End Class
End Namespace
