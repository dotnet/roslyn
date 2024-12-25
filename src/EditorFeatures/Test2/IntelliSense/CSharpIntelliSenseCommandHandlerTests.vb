' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class CSharpIntelliSenseCommandHandlerTests

        <WpfTheory, CombinatorialData>
        Public Async Function TestOpenParenDismissesCompletionAndBringsUpSignatureHelp1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        $$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Go")
                Await state.AssertCompletionSession()
                Await state.AssertNoSignatureHelpSession()
                state.SendTypeChars("(")
                ' Await state.AssertNoCompletionSession() TODO: Split into 2 tests
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
                Assert.Contains("Goo(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543913")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestEscapeDismissesCompletionFirst(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        $$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Goo(a")
                Await state.AssertCompletionSession()
                Await state.AssertSignatureHelpSession()
                state.SendEscape()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
                state.SendEscape()
                Await state.AssertNoCompletionSession()
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531149")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCutDismissesCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        $$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("Goo(a")
                Await state.AssertCompletionSession()
                Await state.AssertSignatureHelpSession()
                state.SendCut()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531149")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestPasteDismissesCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        $$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Goo(a")
                Await state.AssertCompletionSession()
                Await state.AssertSignatureHelpSession()
                state.SendPaste()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function
    End Class
End Namespace
