' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#If False Then
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus
Imports Microsoft.VisualStudio.Text
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus
    Public Class ContainedDocumentTests_AdjustIndentation
        Inherits AbstractContainedDocumentTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub NoNewLines()
            Dim subjectBuffer =
                <Text>
public class Default
{
    void PreRender()
    {
{|S1:[|int x = 1;|]|}
    }
}
                </Text>.NormalizedValue

            Dim spansToAdjust = {0}
            Dim baseIndentations = {3}

            Dim startOfIndent = subjectBuffer.IndexOf("{|S1")
            AssertAdjustIndentation(HtmlMarkup, subjectBuffer, spansToAdjust, baseIndentations, Enumerable.Empty(Of TextChange)(), LanguageNames.CSharp)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub OnSingleLine()
            Dim subjectBuffer =
                <Text>
public class Default
{
    void PreRender()
    {
#line "Foo.aspx", 1{|S1:[|
int x = 1;
|]|}#line hidden
#line default
    }
}
                </Text>.NormalizedValue

            Dim spansToAdjust = {0}
            Dim baseIndentations = {3}

            Dim startOfIndent = subjectBuffer.IndexOf("{|S1") + vbCrLf.Length
            AssertAdjustIndentation(HtmlMarkup, subjectBuffer, spansToAdjust, baseIndentations, {New TextChange(New TextSpan(startOfIndent, 0), "       ")}, LanguageNames.CSharp)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub OnMultipleLines()
            Dim subjectBuffer =
                <Text>
public class Default
{
    void PreRender()
    {
#line "Foo.aspx", 1{|S1:[|
if(true)
{
}
|]|}#line hidden
#line default
    }
}
                </Text>.NormalizedValue

            Dim spansToAdjust = {0}
            Dim baseIndentations = {3}

            ' Span start computation explained:
            ' ---------------------------------
            ' Although this span start computation is ugly to look at, 
            ' this is far better than just saying xx and is more readable.
            Dim startOfLine1Indent = subjectBuffer.IndexOf("{|S1:") + vbCrLf.Length
            Dim startOfLine2Indent = subjectBuffer.IndexOf("true)") - "{|S1:[|".Length + "true)".Length + vbCrLf.Length
            Dim startOfLine3Indent = startOfLine2Indent + vbCrLf.Length + "{".Length

            ' Span length computation explained:
            ' ----------------------------------
            ' The length of the span being edited (replaced) is the indentation on the line.
            ' By outdenting all lines under test, we could just say 0 for span length. 
            ' The edit for all lines except the very first line would also replace the previous newline
            ' So, the length of the span being replaced should be 0 for line 1 and 1 for everything else.

            ' Verify that all statements align with base.
            AssertAdjustIndentation(HtmlMarkup,
                                    subjectBuffer,
                                    spansToAdjust,
                                    baseIndentations,
                                    {New TextChange(New TextSpan(startOfLine1Indent, 0), "       "),
                                     New TextChange(New TextSpan(startOfLine2Indent, 0), "       "),
                                     New TextChange(New TextSpan(startOfLine3Indent, 0), "       ")},
                                    LanguageNames.CSharp)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub IndentationInNestedStatements()
            Dim subjectBuffer =
                <Text>
public class Default
{
    void PreRender()
    {
#line "Foo.aspx", 1{|S1:[|
if(true)
{
Console.WriteLine(5);
}
|]|}#line hidden
#line default
    }
}
                </Text>.NormalizedValue

            Dim spansToAdjust = {0}
            Dim baseIndentations = {3}

            Dim startOfLine1Indent = subjectBuffer.IndexOf("{|S1:") + vbCrLf.Length
            Dim startOfLine2Indent = subjectBuffer.IndexOf("(true)") - "{|S1:[|".Length + "(true)".Length + vbCrLf.Length
            Dim startOfLine3Indent = startOfLine2Indent + vbCrLf.Length + "{".Length
            Dim startOfLine4Indent = subjectBuffer.IndexOf(");") - "{|S1:[|".Length + ");".Length + vbCrLf.Length

            ' Assert that the statement inside the if block is indented 4 spaces from the base which is at column 3.
            ' the default indentation is 4 spaces and this test isn't changing that.
            AssertAdjustIndentation(HtmlMarkup,
                                    subjectBuffer,
                                    spansToAdjust,
                                    baseIndentations,
                                    {New TextChange(New TextSpan(startOfLine1Indent, 0), "       "),
                                     New TextChange(New TextSpan(startOfLine2Indent, 0), "       "),
                                     New TextChange(New TextSpan(startOfLine3Indent, 0), "           "),
                                     New TextChange(New TextSpan(startOfLine4Indent, 0), "       ")},
                                    LanguageNames.CSharp)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub InQuery()
            Dim subjectBuffer =
                <Text>
public class Default
{
    void PreRender()
    {
#line "Foo.aspx", 1{|S1:[|
int[] numbers = { 5, 4, 1 };
var even = from n in numbers
where n % 2 == 0
select n;
|]|}#line hidden
#line default
    }
}
                </Text>.NormalizedValue

            Dim spansToAdjust = {0}
            Dim baseIndentations = {3}

            Dim startOfLine1Indent = subjectBuffer.IndexOf("{|S1:") + vbCrLf.Length
            Dim startOfLine2Indent = subjectBuffer.IndexOf("};") - "{|S1:[|".Length + "};".Length + vbCrLf.Length
            Dim startOfLine3Indent = subjectBuffer.IndexOf("where") - "{|S1:[|".Length
            Dim startOfLine4Indent = subjectBuffer.IndexOf("== 0") - "{|S1:[|".Length + "== 0".Length + vbCrLf.Length

            ' The where and select clauses should be right under from after applying a base indent of 3 to all of those.
            '   var even = from n in numbers
            '              where n % 2 == 0
            '              select n;
            AssertAdjustIndentation(HtmlMarkup,
                                    subjectBuffer,
                                    spansToAdjust,
                                    baseIndentations,
                                    {New TextChange(New TextSpan(startOfLine1Indent, 0), "       "),
                                     New TextChange(New TextSpan(startOfLine2Indent, 0), "       "),
                                     New TextChange(New TextSpan(startOfLine3Indent, 0), "                  "),
                                     New TextChange(New TextSpan(startOfLine4Indent, 0), "                  ")},
                                    LanguageNames.CSharp)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub AtEndOfSpan()
            Dim subjectBuffer =
                <Text>
public class Default
{
    void PreRender()
       {{|S1:[|
       int x = 1;
|]|}            
            }
}
                </Text>.NormalizedValue

            Dim spansToAdjust = {0}
            Dim baseIndentations = {3}
            Dim expected As TextChange = New TextChange(New TextSpan(66, 0), "    ")
            AssertAdjustIndentation(HtmlMarkup, subjectBuffer, spansToAdjust, baseIndentations, {expected}, LanguageNames.CSharp)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus), WorkItem(529885)>
        Public Sub EndInSpan()
            Dim subjectBuffer =
                <Text>
public class Default
{
    void PreRender()
    {    {|S1:[|
        int x = 1;
|]|}
    }
}
                </Text>.NormalizedValue

            Dim spansToAdjust = {0}
            Dim baseIndentations = {3}

            Dim expected = New TextChange(TextSpan.FromBounds(60, 68), "           ")
            AssertAdjustIndentation(HtmlMarkup, subjectBuffer, spansToAdjust, baseIndentations, {expected}, LanguageNames.CSharp)
        End Sub

        Private Sub AssertAdjustIndentation(
            surfaceBufferMarkup As String,
            subjectBufferMarkup As String,
            spansToAdjust As IEnumerable(Of Integer),
            baseIndentations As IEnumerable(Of Integer),
            expectedEdits As IEnumerable(Of TextChange),
            language As String)

            Assert.Equal(spansToAdjust.Count, baseIndentations.Count)

            Using Workspace = GetWorkspace(subjectBufferMarkup, language)

                Dim projectedDocument = Workspace.CreateProjectionBufferDocument(surfaceBufferMarkup, Workspace.Documents, language)
                Dim hostDocument = Workspace.Documents.Single()
                Dim spans = hostDocument.SelectedSpans

                Dim actualEdits As New List(Of TextChange)
                Dim textEdit As New Mock(Of ITextEdit)
                textEdit.
                    Setup(Function(e) e.Replace(It.IsAny(Of Span)(), It.IsAny(Of String)())).
                    Callback(Sub(span As Span, text As String) actualEdits.Add(New TextChange(New TextSpan(span.Start, span.Length), text)))

                For Each index In spansToAdjust
                    ContainedDocument.AdjustIndentationForSpan(GetDocument(Workspace),
                                                               hostDocument.GetTextBuffer().CurrentSnapshot,
                                                               textEdit.Object,
                                                               spans.Item(index),
                                                               baseIndentations.ElementAt(index))
                Next

                AssertEx.Equal(expectedEdits, actualEdits)
            End Using
        End Sub
    End Class
End Namespace
#End If
