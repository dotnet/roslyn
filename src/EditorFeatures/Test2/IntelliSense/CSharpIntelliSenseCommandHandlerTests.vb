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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertNoSignatureHelpSession().ConfigureAwait(True)
                state.SendTypeChars("(")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()").ConfigureAwait(True)
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.SendEscape()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.SendEscape()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Await state.AssertNoSignatureHelpSession().ConfigureAwait(True)
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.SendCut()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.SendPaste()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Function
    End Class
End Namespace
