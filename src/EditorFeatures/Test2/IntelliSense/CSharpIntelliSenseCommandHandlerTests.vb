' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Completion
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class CSharpIntelliSenseCommandHandlerTests
        <WpfFact>
        Public Async Function TestOpenParenDismissesCompletionAndBringsUpSignatureHelp1() As Task
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
                Await state.AssertCompletionSession()
                Await state.AssertNoSignatureHelpSession()
                state.SendTypeChars("(")
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
                Assert.Contains("Foo(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543913)>
        <WpfFact>
        Public Async Function TestEscapeDismissesCompletionFirst() As Task
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
                Await state.AssertCompletionSession()
                Await state.AssertSignatureHelpSession()
                Await state.WaitForAsynchronousOperationsAsync()
                state.SendEscape()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
                Await state.WaitForAsynchronousOperationsAsync()
                state.SendEscape()
                Await state.AssertNoCompletionSession()
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WorkItem(531149)>
        <WpfFact>
        Public Async Function TestCutDismissesCompletion() As Task
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
                Await state.AssertCompletionSession()
                Await state.AssertSignatureHelpSession()
                Await state.WaitForAsynchronousOperationsAsync()
                state.SendCut()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WorkItem(531149)>
        <WpfFact>
        Public Async Function TestPasteDismissesCompletion() As Task
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
                Await state.AssertCompletionSession()
                Await state.AssertSignatureHelpSession()
                Await state.WaitForAsynchronousOperationsAsync()
                state.SendPaste()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function
    End Class
End Namespace
