' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

#If False Then
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Moq
Imports Roslyn.Test.EditorUtilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus
    Public Class ContainedDocumentTests_UpdateText
        Inherits AbstractContainedDocumentTests

        ' TODO: Tests that involve:
        ' 1. Multiple incoming edits and visible spans.
        ' 2. Tests that change the length, thus moving spans in the new text.

        ''' <summary>
        ''' Change starts and ends before visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeBeforeVisibleSpan()
            Dim edits = {New TextChange(TextSpan.FromBounds(1, 2), "ab")}
            AssertEditsApplied("012[|34|]56789", edits, edits)
        End Sub

        ''' <summary>
        ''' Multiple Edits start and end before visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub MultipleEditsBeforeVisibleSpan()
            Dim edits = {
                New TextChange(TextSpan.FromBounds(1, 2), "a"),
                New TextChange(TextSpan.FromBounds(2, 4), "bc")
            }

            AssertEditsApplied("01234[|567|]89", edits, edits)
        End Sub

        ''' <summary>
        ''' Multiple conflicting edits start and end before visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub MultipleConflictingEditsBeforeVisibleSpan()
            Dim edits = {
                New TextChange(TextSpan.FromBounds(1, 2), "a"),
                New TextChange(TextSpan.FromBounds(1, 2), "b")
            }

            AssertEditsApplied("01234[|567|]89", edits, edits)
        End Sub

        ''' <summary>
        ''' Change starts before and ends at start of visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeEndsAtStartOfVisibleSpan()
            Dim edits = {New TextChange(TextSpan.FromBounds(2, 3), "ab")}
            AssertEditsApplied("012[|34|]56789", edits, edits)
        End Sub

        ''' <summary>
        ''' Change starts before and ends at start of visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeEndsAtStartOfVisibleSpanWithWhitespace()
            Dim edits = {New TextChange(TextSpan.FromBounds(4, 8), " a ")}
            Dim expected = {New TextChange(TextSpan.FromBounds(5, 7), "a")}
            Dim input = <Text>01
[| 23 |]
456789</Text>.Value
            AssertEditsApplied(input, expected, edits)
        End Sub

        ''' <summary>
        ''' Change starts before and ends inside visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsBeforeAndEndsInVisibleSpan()
            Dim expected = {
                New TextChange(TextSpan.FromBounds(0, 5), "01a" & vbCrLf),
                New TextChange(TextSpan.FromBounds(5, 7), "b4"),
                New TextChange(TextSpan.FromBounds(7, 14), vbCrLf & "56789")
            }

            Dim input = <Text>012
[|34|]
56789</Text>.Value

            Dim result = <Text>01a
[|b4|]
56789</Text>.Value

            AssertEditsSplit(input, result, {New TextChange(TextSpan.FromBounds(2, 6), "a" & vbCrLf & "b")}, expected)
        End Sub

        ''' <summary>
        ''' Change starts before and ends inside visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsBeforeAndEndsInVisibleSpanChangingLength()
            Dim edits = {New TextChange(TextSpan.FromBounds(2, 6), "a" & vbCrLf & "bc")}
            Dim expected = {
                New TextChange(TextSpan.FromBounds(0, 5), "01a" & vbCrLf),
                New TextChange(TextSpan.FromBounds(5, 7), "bc4"),
                New TextChange(TextSpan.FromBounds(7, 14), vbCrLf & "56789")
            }
            Dim visibleSpan = TextSpan.FromBounds(3, 6)

            Dim input = <Text>012
[|34|]
56789</Text>.Value

            Dim result = <Text>01a
[|bc4|]
56789</Text>.Value

            AssertEditsSplit(input, result, edits, expected)
        End Sub

        ''' <summary>
        ''' Change starts before and ends at end of visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsBeforeAndEndsAtEndOfVisibleSpan()
            Dim edits = {New TextChange(TextSpan.FromBounds(2, 7), "a" & vbCrLf & "bc")}

            Dim expected = {
                New TextChange(TextSpan.FromBounds(0, 5), "01a" & vbCrLf),
                New TextChange(TextSpan.FromBounds(5, 7), "bc"),
                New TextChange(TextSpan.FromBounds(7, 14), vbCrLf & "56789")
            }

            Dim input = <Text>012
[|34|]
56789</Text>.Value

            Dim result = <Text>01a
[|bc|]
56789</Text>.Value

            AssertEditsSplit(input, result, edits, expected)
        End Sub

        ''' <summary>
        ''' Change starts before and ends after visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeEncompassesVisibleSpan()
            Dim edits = {New TextChange(TextSpan.FromBounds(2, 11), "a" & vbCrLf & "bc" & vbCrLf & "de")}

            Dim expected = {
                New TextChange(TextSpan.FromBounds(0, 5), "01a" & vbCrLf),
                New TextChange(TextSpan.FromBounds(5, 7), "bc"),
                New TextChange(TextSpan.FromBounds(7, 14), vbCrLf & "de789")
            }

            Dim input = <Text>012
[|34|]
56789</Text>.Value

            Dim result = <Text>01a
[|bc|]
de789</Text>.Value

            AssertEditsSplit(input, result, edits, expected)
        End Sub

        ''' <summary>
        ''' Change starts at start of visible span and ends inside visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsAtStartOfVisibleSpanAndEndsInside()
            Dim edits = {New TextChange(TextSpan.FromBounds(3, 4), "a")}
            AssertEditsApplied("012[|34|]56789", edits, edits)
        End Sub

        ''' <summary>
        ''' Change starts at start of visible span and ends at end of visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeEqualsVisibleSpan()
            Dim edits = {New TextChange(TextSpan.FromBounds(3, 5), "ab")}
            AssertEditsApplied("012[|34|]56789", edits, edits)
        End Sub

        ''' <summary>
        ''' Change starts at start of visible span and ends after visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsAtStartOfVisibleSpanAndEndsAfter()
            Dim edits = {New TextChange(TextSpan.FromBounds(3, 10), vbCrLf & "ab" & vbCrLf & "c")}

            Dim expected = {
                New TextChange(TextSpan.FromBounds(0, 5), "012" & vbCrLf),
                New TextChange(TextSpan.FromBounds(5, 7), "ab"),
                New TextChange(TextSpan.FromBounds(7, 14), vbCrLf & "c6789")
            }

            Dim input = <Text>012
[|34|]
56789</Text>.Value

            Dim result = <Text>012
[|ab|]
c6789</Text>.Value

            AssertEditsSplit(input, result, edits, expected)
        End Sub

        ''' <summary>
        ''' Change starts inside visible span and ends inside visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeIsInsideVisibleSpan()
            Dim edits = {New TextChange(TextSpan.FromBounds(3, 4), "n")}
            AssertEditsApplied("01[|234|]56789", edits, edits)
        End Sub

        ''' <summary>
        ''' Change starts inside visible span and ends at end of visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsInsideVisibleSpanAndEndsAtEnd()
            Dim edits = {New TextChange(TextSpan.FromBounds(4, 5), "a")}
            AssertEditsApplied("012[|34|]56789", edits, edits)
        End Sub

        ''' <summary>
        ''' Change starts inside visible span and ends after visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsInsideVisibleSpanAndEndsAfter()
            Dim edits = {New TextChange(TextSpan.FromBounds(6, 10), "a" & vbCrLf & "b")}

            Dim expected = {
                New TextChange(TextSpan.FromBounds(0, 5), "012" & vbCrLf),
                New TextChange(TextSpan.FromBounds(5, 7), "3a"),
                New TextChange(TextSpan.FromBounds(7, 14), vbCrLf & "b6789")
            }

            Dim input = <Text>012
[|34|]
56789</Text>.Value

            Dim result = <Text>012
[|3a|]
b6789</Text>.Value

            AssertEditsSplit(input, result, edits, expected)
        End Sub

        ''' <summary>
        ''' Change starts at end of visible span and ends after visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsAtEndOfVisibleSpan()
            Dim edits = {New TextChange(TextSpan.FromBounds(5, 6), "a")}
            AssertEditsApplied("012[|34|]56789", edits, edits)
        End Sub

        ''' <summary>
        ''' Change starts after visible span and ends after visible span
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeAfterVisibleSpan()
            Dim edits = {New TextChange(TextSpan.FromBounds(7, 8), "a")}
            AssertEditsApplied("012[|34|]56789", edits, edits)
        End Sub

        ''' <summary>
        ''' Multiple visible spans
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsInsideVisibleSpanAndEndsAfterMultipleVisibleSpans1()
            Dim edits = {New TextChange(TextSpan.FromBounds(1, 8), "a" & vbCrLf & "b" & vbCrLf & "c")}
            Dim expectedEdits = {
                New TextChange(TextSpan.FromBounds(0, 0), ""),
                New TextChange(TextSpan.FromBounds(0, 2), "0a"),
                New TextChange(TextSpan.FromBounds(2, 7), vbCrLf & "b" & vbCrLf),
                New TextChange(TextSpan.FromBounds(7, 10), "c45"),
                New TextChange(TextSpan.FromBounds(10, 16), vbCrLf & "6789")
            }

            Dim input = <Text>[|01|]
2
[|345|]
6789</Text>.Value

            Dim result = <Text>[|0a|]
b
[|c45|]
6789</Text>.Value

            AssertEditsSplit(input, result, edits, expectedEdits)
        End Sub

        ''' <summary>
        ''' Multiple visible spans
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub ChangeStartsInsideVisibleSpanAndEndsAtAfterMultipleVisibleSpans2()
            Dim edits = {New TextChange(TextSpan.FromBounds(1, 6), "a" & vbCrLf & "bc")}
            Dim expectedEdits = {
                New TextChange(TextSpan.FromBounds(0, 0), ""),
                New TextChange(TextSpan.FromBounds(0, 2), "0a"),
                New TextChange(TextSpan.FromBounds(2, 9), vbCrLf & "bc4" & vbCrLf),
                New TextChange(TextSpan.FromBounds(9, 13), "5678"),
                New TextChange(TextSpan.FromBounds(13, 16), vbCrLf & "9")
            }

            Dim input = <Text>[|01|]
234
[|5678|]
9</Text>.Value

            Dim result = <Text>[|0a|]
bc4
[|5678|]
9</Text>.Value

            AssertEditsSplit(input, result, edits, expectedEdits)
        End Sub

        ''' <summary>
        ''' Multiple visible spans and Multiple Edits
        ''' </summary>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub MultipleChangesAndMultipleVisibleSpans1()
            Dim edits = {New TextChange(TextSpan.FromBounds(1, 8), "a" & vbCrLf & "b" & vbCrLf & "c")}
            Dim expectedEdits = {
                New TextChange(TextSpan.FromBounds(0, 0), ""),
                New TextChange(TextSpan.FromBounds(0, 2), "0a"),
                New TextChange(TextSpan.FromBounds(2, 7), vbCrLf & "b" & vbCrLf),
                New TextChange(TextSpan.FromBounds(7, 11), "c456"),
                New TextChange(TextSpan.FromBounds(11, 16), vbCrLf & "789")
            }

            Dim input = <Text>[|01|]
2
[|3456|]
789</Text>.Value

            Dim result = <Text>[|0a|]
b
[|c456|]
789</Text>.Value

            AssertEditsSplit(input, result, edits, expectedEdits)
        End Sub

        <WpfFact, WorkItem(529800), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub LargeAddAndRemove()
            Dim input = <Text>0
[|12|]
3456
[|78|]
9</Text>.Value

            Dim expected = <Text>0
[|1ab2|]
3456
[|78|]
9</Text>.Value

            Dim edits = {
                New TextChange(TextSpan.FromBounds(4, 4), "ab2" & vbCrLf & "3456" & vbCrLf & "7"),
                New TextChange(TextSpan.FromBounds(4, 14), "")
            }

            Dim expectedEdits = {
                New TextChange(TextSpan.FromBounds(0, 3), "0" & vbCrLf),
                New TextChange(TextSpan.FromBounds(3, 5), "1ab2"),
                New TextChange(TextSpan.FromBounds(5, 13), vbCrLf & "3456" & vbCrLf),
                New TextChange(TextSpan.FromBounds(13, 15), "78"),
                New TextChange(TextSpan.FromBounds(15, 18), vbCrLf & "9")
            }

            AssertEditsSplit(input, expected, edits, expectedEdits)
        End Sub

        ''' <summary>
        ''' The point of this test is that when we get a request to replace the entire contents of
        ''' the file, we want to make sure to NOT replace where the boundaries between the projected
        ''' spans are.  In expression scenarios (Razor inline "@" expressions, and &lt;%= or &lt;%:
        ''' nuggets for aspx files, they end up generating code like:
        '''     #line "Goo.aspx", 3
        '''         __o = |expr|;
        '''     
        '''     #line default
        '''     #line hidden
        ''' Where the | mark the boundaries of the projected span.  Unfortunately, we don't know
        ''' where they are in the _new_ text.  All we have to go off is are the #line directives.
        ''' 
        ''' To preserve the boundaries, we also skip whitespace and the "__o = ", as well as the 
        ''' ending newlines and ; if there is one.
        ''' </summary>
        <WpfFact, WorkItem(617816), Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub AtEndOfExpressionNugget()
            Dim inputMarkup = <text>
class C
{
    static void M()
    {
#line "Goo", 1
        [|foreach (var item in new[] { 1, 2, 3 })
        {|]

#line default
#line hidden
#line "Goo", 2
            __o = [|item|]

#line default
#line hidden
#line "Goo", 3
        [|}|]

#line default
#line hidden
    }
}
                        </text>.Value

            Dim resultMarkup = <text>
class C
{
    static void M()
    {
#line "Goo", 1
        [|foreach (var itemss in new[] { 1, 2, 3 })
        {

|]#line default
#line hidden
#line "Goo", 2
            [|__o = itemss;

|]#line default
#line hidden
#line "Goo", 3
        [|}

|]#line default
#line hidden
    }
}
                         </text>.Value

            Dim input As String = Nothing
            Dim spans As IList(Of TextSpan) = Nothing
            MarkupTestFile.GetSpans(inputMarkup.NormalizeLineEndings(), input, spans)

            Dim result As String = Nothing
            MarkupTestFile.GetSpans(resultMarkup.NormalizeLineEndings(), result, spans)

            Dim expectedEdits = {
                New TextChange(TextSpan.FromBounds(0, 58), <text>
class C
{
    static void M()
    {
#line "Goo", 1
        </text>.NormalizedValue),
                New TextChange(TextSpan.FromBounds(66, 116), <text>foreach (var itemss in new[] { 1, 2, 3 })
        {

</text>.NormalizedValue),
                New TextChange(TextSpan.FromBounds(116, 165), <text>#line default
#line hidden
#line "Goo", 2
            </text>.NormalizedValue),
                New TextChange(TextSpan.FromBounds(183, 187), <text>itemss</text>.NormalizedValue),
                New TextChange(TextSpan.FromBounds(187, 236), <text>#line default
#line hidden
#line "Goo", 3
        </text>.NormalizedValue),
                New TextChange(TextSpan.FromBounds(244, 245), <text>}

</text>.NormalizedValue),
                New TextChange(TextSpan.FromBounds(245, 312), <text>#line default
#line hidden
    }
}
                         </text>.NormalizedValue)}

            AssertEditsSplit(
                inputMarkup,
                resultMarkup,
                {New TextChange(New TextSpan(0, input.Length), result)},
                expectedEdits)
        End Sub

        Private Sub AssertEditsSplit(
            inputMarkup As String,
            resultingMarkup As String,
            edits As IEnumerable(Of TextChange),
            expectedEdits As IEnumerable(Of TextChange))

            Dim visibleSpansInOriginalText As IList(Of TextSpan) = Nothing
            Dim originalText As String = Nothing
            MarkupTestFile.GetSpans(inputMarkup.NormalizeLineEndings(), originalText, visibleSpansInOriginalText)

            Dim visibleSpansInNewText As IList(Of TextSpan) = Nothing
            Dim finalText As String = Nothing
            MarkupTestFile.GetSpans(resultingMarkup.NormalizeLineEndings(), finalText, visibleSpansInNewText)

            Dim actualEdits As New List(Of TextChange)
            Dim textEdit As New Mock(Of ITextEdit)
            textEdit.
                Setup(Function(e) e.Replace(It.IsAny(Of Span)(), It.IsAny(Of String)())).
                Callback(Sub(span As Span, text As String) actualEdits.Add(New TextChange(New TextSpan(span.Start, span.Length), text)))

            Dim buffer = EditorFactory.CreateBuffer(TestExportProvider.ExportProvider, originalText)
            Dim originalSnapshot = buffer.CurrentSnapshot
            textEdit.SetupGet(Function(e) e.Snapshot).Returns(originalSnapshot)

            Dim newSnapshot As ITextSnapshot
            Using edit = buffer.CreateEdit()
                For Each change In edits
                    edit.Replace(change.Span.ToSpan(), change.NewText)
                Next

                newSnapshot = edit.Apply()
            End Using

            Assert.Equal(finalText, newSnapshot.GetText())

            Dim affectedVisibleSpans As IEnumerable(Of Integer) = Nothing

            ContainedDocument.ApplyChanges(
                textEdit.Object,
                edits,
                visibleSpansInOriginalText,
                affectedVisibleSpans)

            actualEdits.Clear()
            ContainedDocument.ApplyEditsByRegion(
                originalSnapshot,
                textEdit.Object,
                visibleSpansInOriginalText,
                newSnapshot.AsText(),
                visibleSpansInNewText)

            AssertEx.Equal(expectedEdits, actualEdits)
        End Sub

        Private Sub AssertEditsApplied(
            markup As String,
            expectedEdits As IEnumerable(Of TextChange),
            textChanges As IList(Of TextChange))

            Dim visibleSpansInOriginalText As IList(Of TextSpan) = Nothing
            Dim originalText As String = Nothing
            MarkupTestFile.GetSpans(markup.NormalizeLineEndings(), originalText, visibleSpansInOriginalText)

            Dim actualEdits As New List(Of TextChange)
            Dim textEdit As New Mock(Of ITextEdit)
            textEdit.
                Setup(Function(e) e.Replace(It.IsAny(Of Span)(), It.IsAny(Of String)())).
                Callback(Sub(span As Span, text As String) actualEdits.Add(New TextChange(New TextSpan(span.Start, span.Length), text)))

            Dim buffer = EditorFactory.CreateBuffer(TestExportProvider.ExportProvider, originalText)
            textEdit.SetupGet(Function(e) e.Snapshot).Returns(buffer.CurrentSnapshot)

            Dim affectedVisibleSpans As IEnumerable(Of Integer) = Nothing
            Dim result = ContainedDocument.TryApplyChanges(
                textEdit.Object,
                textChanges,
                visibleSpansInOriginalText,
                affectedVisibleSpans)

            Assert.True(result)
            AssertEx.Equal(expectedEdits, actualEdits)
        End Sub
    End Class
End Namespace
#End If
