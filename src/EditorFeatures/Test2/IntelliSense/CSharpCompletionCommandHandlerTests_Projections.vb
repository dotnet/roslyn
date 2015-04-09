' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.VisualStudio.Text.Projection

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class CSharpCompletionCommandHandlerTests_Projections

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestSimpleWithJustSubjectBuffer()
            Using state = TestState.CreateCSharpTestState(
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
                state.AssertSelectedCompletionItem(displayText:="CurrentDomain")
                state.SendTab()
                Assert.Contains("__o = AppDomain.CurrentDomain", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestAfterDot()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
{|S2:
class C
{
    void Foo()
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
                state.AssertCompletionSession()

                state.SendTypeCharsToSpecificViewAndBuffer("Cons", view, buffer)
                state.AssertSelectedCompletionItem(displayText:="Console")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestInObjectCreationExpression()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
{|S2:
class C
{
    void Foo()
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
                state.AssertSelectedCompletionItem(displayText:="string", isHardSelected:=True)
            End Using
        End Sub

        <WorkItem(771761)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestRegionCompletionCommitFormatting()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
{|S2:
class C
{
    void Foo()
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
                state.AssertSelectedCompletionItem(displayText:="region", shouldFormatOnCommit:=True)

            End Using
        End Sub
    End Class
End Namespace
