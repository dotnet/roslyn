' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpIntelliSenseCommandHandlerTests

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
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
                Await state.AssertCompletionSessionAsync()
                Await state.AssertNoSignatureHelpSessionAsync()
                state.SendTypeChars("(")
                ' Await state.AssertNoCompletionSession() TODO: Split into 2 tests
                Await state.AssertSignatureHelpSessionAsync()
                Await state.AssertSelectedSignatureHelpItemAsync(displayText:="void C.Goo()")
                Assert.Contains("Goo(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543913")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
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
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSignatureHelpSessionAsync()
                state.SendEscape()
                Await state.AssertNoCompletionSessionAsync()
                Await state.AssertSignatureHelpSessionAsync()
                state.SendEscape()
                Await state.AssertNoCompletionSessionAsync()
                Await state.AssertNoSignatureHelpSessionAsync()
            End Using
        End Function

        <WorkItem(531149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531149")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
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
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSignatureHelpSessionAsync()
                state.SendCut()
                Await state.AssertNoCompletionSessionAsync()
                Await state.AssertSignatureHelpSessionAsync()
            End Using
        End Function

        <WorkItem(531149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531149")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
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
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSignatureHelpSessionAsync()
                state.SendPaste()
                Await state.AssertNoCompletionSessionAsync()
                Await state.AssertSignatureHelpSessionAsync()
            End Using
        End Function
    End Class
End Namespace
