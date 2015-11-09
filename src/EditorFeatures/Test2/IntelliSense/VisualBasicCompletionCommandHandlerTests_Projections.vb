' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.VisualStudio.Text.Projection

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class VisualBasicCompletionCommandHandlerTests_Projections

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSimpleWithJustSubjectBuffer() As Task
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTypeChars("Curr")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="CurrentDomain").ConfigureAwait(True)
                state.SendTab()
                Assert.Contains("__o = AppDomain.CurrentDomain", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterDot() As Task
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
                Await state.AssertCompletionSession().ConfigureAwait(True)

                state.SendTypeCharsToSpecificViewAndBuffer("Cons", view, buffer)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Console").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInObjectCreationExpression() As Task
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
                Await state.AssertCompletionSession().ConfigureAwait(True)

                state.SendTypeCharsToSpecificViewAndBuffer("Str", view, buffer)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="String", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function
    End Class
End Namespace
