' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.VisualStudio.Text.Projection

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class CSharpCompletionCommandHandlerTests_Projections

        <WpfTheory, CombinatorialData>
        Public Async Function TestSimpleWithJustSubjectBuffer(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

public class _Page_Default_cshtml : System.Web.WebPages.WebPage {
private static object @__o;
#line hidden

public override void Execute() {

#line 1 "Default.cshtml"
               __o = AppDomain$$
#line default
#line hidden
}
}]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".Curr")
                Await state.AssertSelectedCompletionItem(displayText:="CurrentDomain")
                state.SendTab()
                Assert.Contains("__o = AppDomain.CurrentDomain", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestAfterDot(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
{|S2:
class C
{
    void Goo()
    {
        System$$
    }
}
|}]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                Dim subjectDocument = state.Workspace.Documents.First()
                Dim firstProjection = state.Workspace.CreateProjectionBufferDocument(
                    <Document>
{|S1: &lt;html&gt;@|}
{|S2:|}
                    </Document>.NormalizedValue, {subjectDocument}, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim topProjectionBuffer = state.Workspace.CreateProjectionBufferDocument(
                <Document>
{|S1:|}
{|S2:&lt;/html&gt;|}
                              </Document>.NormalizedValue, {firstProjection}, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim view = topProjectionBuffer.GetTextView()
                Dim buffer = subjectDocument.GetTextBuffer()

                state.SendTypeCharsToSpecificViewAndBuffer(".", view, buffer)
                Await state.AssertCompletionSession(view)

                state.SendTypeCharsToSpecificViewAndBuffer("Cons", view, buffer)
                Await state.AssertSelectedCompletionItem(displayText:="Console", projectionsView:=view)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestInObjectCreationExpression(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
{|S2:
class C
{
    void Goo()
    {
        string s = new$$
    }
}
|}]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                Dim subjectDocument = state.Workspace.Documents.First()
                Dim firstProjection = state.Workspace.CreateProjectionBufferDocument(
                    <Document>
{|S1: &lt;html&gt;@|}
{|S2:|}
                    </Document>.NormalizedValue, {subjectDocument}, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim topProjectionBuffer = state.Workspace.CreateProjectionBufferDocument(
                <Document>
{|S1:|}
{|S2:&lt;/html&gt;|}
                              </Document>.NormalizedValue, {firstProjection}, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim view = topProjectionBuffer.GetTextView()
                Dim buffer = subjectDocument.GetTextBuffer()

                state.SendTypeCharsToSpecificViewAndBuffer(" ", view, buffer)
                Await state.AssertSelectedCompletionItem(displayText:="string", isHardSelected:=True, projectionsView:=view)
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestRegionCompletionCommitFormatting(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
{|S2:
class C
{
    void Goo()
    {
        $$
    }
}
|}]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                Dim subjectDocument = state.Workspace.Documents.First()
                Dim firstProjection = state.Workspace.CreateProjectionBufferDocument(
                    <Document>
{|S1: &lt;html&gt;@|}
{|S2:|}
                    </Document>.NormalizedValue, {subjectDocument}, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim topProjectionBuffer = state.Workspace.CreateProjectionBufferDocument(
                <Document>
{|S1:|}
{|S2:&lt;/html&gt;|}
                              </Document>.NormalizedValue, {firstProjection}, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim view = topProjectionBuffer.GetTextView()
                Dim buffer = subjectDocument.GetTextBuffer()

                state.SendTypeCharsToSpecificViewAndBuffer("#reg", view, buffer)
                Await state.AssertSelectedCompletionItem(displayText:="region", shouldFormatOnCommit:=True, projectionsView:=view)
            End Using
        End Function
    End Class
End Namespace
