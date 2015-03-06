' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Completion
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class CSharpIntelliSenseCommandHandlerTests
        <Fact>
        Public Sub TestOpenParenDismissesCompletionAndBringsUpSignatureHelp1()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Foo()
    {
        $$
    }
}
                              </Document>)

                state.SendTypeChars("Fo")
                state.AssertCompletionSession()
                state.AssertNoSignatureHelpSession()
                state.SendTypeChars("(")
                state.AssertNoCompletionSession()
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
                Assert.Contains("Foo(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(543913)>
        <Fact>
        Public Sub TestEscapeDismissesCompletionFirst()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Foo()
    {
        $$
    }
}
                              </Document>)

                state.SendTypeChars("Foo(a")
                state.AssertCompletionSession()
                state.AssertSignatureHelpSession()
                state.SendEscape(block:=True)
                state.AssertNoCompletionSession()
                state.AssertSignatureHelpSession()
                state.SendEscape(block:=True)
                state.AssertNoCompletionSession()
                state.AssertNoSignatureHelpSession()
            End Using
        End Sub

        <WorkItem(531149)>
        <Fact>
        Public Sub TestCutDismissesCompletion()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Foo()
    {
        $$
    }
}
                              </Document>)
                state.SendTypeChars("Foo(a")
                state.AssertCompletionSession()
                state.AssertSignatureHelpSession()
                state.SendCut(block:=True)
                state.AssertNoCompletionSession()
                state.AssertSignatureHelpSession()
            End Using
        End Sub

        <WorkItem(531149)>
        <Fact>
        Public Sub TestPasteDismissesCompletion()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Foo()
    {
        $$
    }
}
                              </Document>)

                state.SendTypeChars("Foo(a")
                state.AssertCompletionSession()
                state.AssertSignatureHelpSession()
                state.SendPaste(block:=True)
                state.AssertNoCompletionSession()
                state.AssertSignatureHelpSession()
            End Using
        End Sub
    End Class
End Namespace
