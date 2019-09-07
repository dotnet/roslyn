' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.VisualStudio.Text.Projection

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_Projections

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSimpleWithJustSubjectBuffer() As Task
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
}]]></Document>)

                state.SendTypeChars(".Curr")
                Await state.AssertSelectedCompletionItem(displayText:="CurrentDomain")
                state.SendTab()
                Assert.Contains("__o = AppDomain.CurrentDomain", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterDot() As Task
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
|}]]></Document>)
                Dim subjectDocument = state.Workspace.Documents.First()
                Dim firstProjection = state.Workspace.CreateProjectionBufferDocument(
                    <Document>
{|S1: &lt;html&gt;@|}
{|S2:|}
                    </Document>.NormalizedValue, {subjectDocument}, LanguageNames.CSharp, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim topProjectionBuffer = state.Workspace.CreateProjectionBufferDocument(
                <Document>
{|S1:|}
{|S2:&lt;/html&gt;|}
                              </Document>.NormalizedValue, {firstProjection}, LanguageNames.CSharp, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim view = topProjectionBuffer.GetTextView()
                Dim buffer = subjectDocument.GetTextBuffer()

                state.SendTypeCharsToSpecificViewAndBuffer(".", view, buffer)
                Await state.AssertCompletionSession(view)

                state.SendTypeCharsToSpecificViewAndBuffer("Cons", view, buffer)
                Await state.AssertSelectedCompletionItem(displayText:="Console", projectionsView:=view)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInObjectCreationExpression() As Task
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
|}]]></Document>)
                Dim subjectDocument = state.Workspace.Documents.First()
                Dim firstProjection = state.Workspace.CreateProjectionBufferDocument(
                    <Document>
{|S1: &lt;html&gt;@|}
{|S2:|}
                    </Document>.NormalizedValue, {subjectDocument}, LanguageNames.CSharp, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim topProjectionBuffer = state.Workspace.CreateProjectionBufferDocument(
                <Document>
{|S1:|}
{|S2:&lt;/html&gt;|}
                              </Document>.NormalizedValue, {firstProjection}, LanguageNames.CSharp, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim view = topProjectionBuffer.GetTextView()
                Dim buffer = subjectDocument.GetTextBuffer()

                state.SendTypeCharsToSpecificViewAndBuffer(" ", view, buffer)
                Await state.AssertSelectedCompletionItem(displayText:="string", isHardSelected:=True, projectionsView:=view)
            End Using
        End Function

        <WorkItem(771761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRegionCompletionCommitFormatting() As Task
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
|}]]></Document>)
                Dim subjectDocument = state.Workspace.Documents.First()
                Dim firstProjection = state.Workspace.CreateProjectionBufferDocument(
                    <Document>
{|S1: &lt;html&gt;@|}
{|S2:|}
                    </Document>.NormalizedValue, {subjectDocument}, LanguageNames.CSharp, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim topProjectionBuffer = state.Workspace.CreateProjectionBufferDocument(
                <Document>
{|S1:|}
{|S2:&lt;/html&gt;|}
                              </Document>.NormalizedValue, {firstProjection}, LanguageNames.CSharp, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim view = topProjectionBuffer.GetTextView()
                Dim buffer = subjectDocument.GetTextBuffer()

                state.SendTypeCharsToSpecificViewAndBuffer("#reg", view, buffer)
                Await state.AssertSelectedCompletionItem(displayText:="region", shouldFormatOnCommit:=True, projectionsView:=view)
            End Using
        End Function
    End Class
End Namespace
