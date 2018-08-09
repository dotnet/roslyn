' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpIntelliSenseCommandHandlerTests

        Private Shared Function GetAllCompletions() As IEnumerable(Of Object())
            Return TestStateFactory.GetAllCompletions()
        End Function

        <MemberData(NameOf(GetAllCompletions))> <WpfTheory>
        Public Async Function TestOpenParenDismissesCompletionAndBringsUpSignatureHelp1(completion As Completions) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completion,
                              <Document>
class C
{
    void Goo()
    {
        $$
    }
}
                              </Document>)

                state.SendTypeChars("Go")
                Await state.AssertCompletionSession()
                Await state.AssertNoSignatureHelpSession()
                state.SendTypeChars("(")
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
                Assert.Contains("Goo(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543913")>
        <MemberData(NameOf(GetAllCompletions))> <WpfTheory>
        Public Async Function TestEscapeDismissesCompletionFirst(completion As Completions) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completion,
                              <Document>
class C
{
    void Goo()
    {
        $$
    }
}
                              </Document>)

                state.SendTypeChars("Goo(a")
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

        <WorkItem(531149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531149")>
        <MemberData(NameOf(GetAllCompletions))> <WpfTheory>
        Public Async Function TestCutDismissesCompletion(completion As Completions) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completion,
                              <Document>
class C
{
    void Goo()
    {
        $$
    }
}
                              </Document>)
                state.SendTypeChars("Goo(a")
                Await state.AssertCompletionSession()
                Await state.AssertSignatureHelpSession()
                Await state.WaitForAsynchronousOperationsAsync()
                state.SendCut()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WorkItem(531149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531149")>
        <MemberData(NameOf(GetAllCompletions))> <WpfTheory>
        Public Async Function TestPasteDismissesCompletion(completion As Completions) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completion,
                              <Document>
class C
{
    void Goo()
    {
        $$
    }
}
                              </Document>)

                state.SendTypeChars("Goo(a")
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
