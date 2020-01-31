' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
#If TODO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Differencing
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Projection
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus
    <UseExportProvider>
    Public Class ContainedDocumentTests_AdjustIndentation

        Private Shared ReadOnly s_htmlMarkup As String = "
<html>
    <body>
        <%{|S1:|}%>
    </body>
</html>".NormalizeLineEndings

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
            AssertAdjustIndentation(s_htmlMarkup, subjectBuffer, spansToAdjust, baseIndentations, Enumerable.Empty(Of TextChange)(), LanguageNames.CSharp)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub OnSingleLine()
            Dim subjectBuffer =
                <Text>
public class Default
{
    void PreRender()
    {
#line "Goo.aspx", 1{|S1:[|
int x = 1;
|]|}#line hidden
#line default
    }
}
                </Text>.NormalizedValue

            Dim spansToAdjust = {0}
            Dim baseIndentations = {3}

            Dim startOfIndent = subjectBuffer.IndexOf("{|S1") + vbCrLf.Length
            AssertAdjustIndentation(s_htmlMarkup, subjectBuffer, spansToAdjust, baseIndentations, {New TextChange(New TextSpan(startOfIndent, 0), "       ")}, LanguageNames.CSharp)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub OnMultipleLines()
            Dim subjectBuffer =
                <Text>
public class Default
{
    void PreRender()
    {
#line "Goo.aspx", 1{|S1:[|
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
            AssertAdjustIndentation(s_htmlMarkup,
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
#line "Goo.aspx", 1{|S1:[|
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
            AssertAdjustIndentation(s_htmlMarkup,
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
#line "Goo.aspx", 1{|S1:[|
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
            AssertAdjustIndentation(s_htmlMarkup,
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
            AssertAdjustIndentation(s_htmlMarkup, subjectBuffer, spansToAdjust, baseIndentations, {expected}, LanguageNames.CSharp)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus), WorkItem(529885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529885")>
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
            AssertAdjustIndentation(s_htmlMarkup, subjectBuffer, spansToAdjust, baseIndentations, {expected}, LanguageNames.CSharp)
        End Sub

        Private Sub AssertAdjustIndentation(
            surfaceBufferMarkup As String,
            subjectBufferMarkup As String,
            spansToAdjust As IEnumerable(Of Integer),
            baseIndentations As IEnumerable(Of Integer),
            expectedEdits As IEnumerable(Of TextChange),
            language As String)

            Assert.Equal(spansToAdjust.Count, baseIndentations.Count)

            Dim editorOptionsFactoryService = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExport(Of IEditorOptionsFactoryService)().Value
            Dim differenceSelectorService = TestExportProvider.ExportProviderWithCSharpAndVisualBasic.GetExport(Of ITextDifferencingSelectorService)().Value

            Using workspace = TestWorkspace.Create(language, compilationOptions:=Nothing, parseOptions:=Nothing, content:=subjectBufferMarkup)

                Dim languageServices = workspace.Services.GetLanguageServices(language)
                Dim contentTypeService = languageServices.GetService(Of IContentTypeLanguageService)
                Dim syntaxFacts = languageServices.GetService(Of ISyntaxFactsService)
                Dim projectionBufferFactory = workspace.GetService(Of IProjectionBufferFactoryService)

                Dim diffService = If(contentTypeService IsNot Nothing, differenceSelectorService.GetTextDifferencingService(contentTypeService.GetDefaultContentType()), Nothing)
                If diffService Is Nothing Then
                    diffService = differenceSelectorService.DefaultTextDifferencingService
                End If

                Dim projectedDocument = workspace.CreateProjectionBufferDocument(surfaceBufferMarkup, workspace.Documents, language)
                Dim document = workspace.Documents.Single()
                Dim solution = workspace.CurrentSolution
                Dim spans = document.SelectedSpans

                Dim documentBuffer = document.GetTextBuffer()
                Dim surfaceBuffer = projectedDocument.GetTextBuffer()

                Dim userCodeSpan = documentBuffer.CurrentSnapshot.CreateTrackingSpan(New Span(0, documentBuffer.CurrentSnapshot.Length), SpanTrackingMode.EdgeExclusive)
                Dim generatedCode = "
#line hidden
GeneratedCode();  
#line restore
"
                Dim codeBuffer = projectionBufferFactory.CreateProjectionBuffer(projectionEditResolver:=Nothing, {generatedCode, userCodeSpan, generatedCode}, ProjectionBufferOptions.None)

                Dim actualEdits As New List(Of TextChange)
                Dim textEdit As New Mock(Of ITextEdit)
                textEdit.
                    Setup(Function(e) e.Replace(It.IsAny(Of Span)(), It.IsAny(Of String)())).
                    Callback(Sub(span As Span, text As String) actualEdits.Add(New TextChange(New TextSpan(span.Start, span.Length), text)))

                Dim containedBuffers = New ContainedDocumentBuffers(
                    codeBuffer,
                    surfaceBuffer,
                    language,
                    document.Id,
                    diffService,
                    editorOptionsFactoryService,
                    syntaxFacts,
                    vbHelperFormattingRule:=Nothing,
                    hostIndentationProvider:=Nothing)

                containedBuffers.AdjustIndentation(spansToAdjust, solution)

                AssertEx.Equal(expectedEdits, actualEdits)
            End Using
        End Sub
    End Class
End Namespace
#endif 
