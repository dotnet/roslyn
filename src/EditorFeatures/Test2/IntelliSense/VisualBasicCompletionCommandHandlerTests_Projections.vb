' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.VisualStudio.Text.Projection

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class VisualBasicCompletionCommandHandlerTests_Projections

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestSimpleWithJustSubjectBuffer()
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Option Strict Off
Option Explicit On

Imports System

Namespace ASP
    Public Class _Page_Views_Home_Index_vbhtml

        Private Shared __o As Object

        Public Sub Execute()
#ExternalSource ("Index.vbhtml", 1)
            __o = AppDomain$$

#End ExternalSource

        End Sub
    End Class
End Namespace
]]></Document>)

                state.SendTypeChars(".")
                state.AssertCompletionSession()
                state.SendTypeChars("Curr")
                state.AssertSelectedCompletionItem(displayText:="CurrentDomain")
                state.SendTab()
                Assert.Contains("__o = AppDomain.CurrentDomain", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestAfterDot()
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
{|S2:
Class C
    Sub Foo()
        System$$
    End Sub
End Class
|}]]></Document>)
                Dim subjectDocument = state.Workspace.Documents.First()
                Dim firstProjection = state.Workspace.CreateProjectionBufferDocument(
                    <Document>
{|S1: &lt;html&gt;@|}
{|S2:|}
                    </Document>.NormalizedValue, {subjectDocument}, LanguageNames.VisualBasic, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim topProjectionBuffer = state.Workspace.CreateProjectionBufferDocument(
                <Document>
{|S1:|}
{|S2:&lt;/html&gt;|}
                              </Document>.NormalizedValue, {firstProjection}, LanguageNames.VisualBasic, options:=ProjectionBufferOptions.WritableLiteralSpans)


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
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
{|S2:
Class C
    Sub Foo()
        Dim s As New$$
    End Sub
End Class
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
                state.AssertCompletionSession()

                state.SendTypeCharsToSpecificViewAndBuffer("Str", view, buffer)
                state.AssertSelectedCompletionItem(displayText:="String", isHardSelected:=True)
            End Using
        End Sub
    End Class
End Namespace
