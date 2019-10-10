' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpIntelliSenseCommandHandlerTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOpenParenDismissesCompletionAndBringsUpSignatureHelp1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEscapeDismissesCompletionFirst() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
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
                state.SendEscape()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
                state.SendEscape()
                Await state.AssertNoCompletionSession()
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WorkItem(531149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531149")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCutDismissesCompletion() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
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
                state.SendCut()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WorkItem(531149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531149")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPasteDismissesCompletion() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
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
                state.SendPaste()
                Await state.AssertNoCompletionSession()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function
    End Class
End Namespace
