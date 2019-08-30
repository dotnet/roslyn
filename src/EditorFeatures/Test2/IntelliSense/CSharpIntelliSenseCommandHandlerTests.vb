' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpIntelliSenseCommandHandlerTests
        Public Shared ReadOnly Property AllCompletionImplementations() As IEnumerable(Of Object())
            Get
                Return TestStateFactory.GetAllCompletionImplementations()
            End Get
        End Property

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOpenParenDismissesCompletionAndBringsUpSignatureHelp1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEscapeDismissesCompletionFirst(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCutDismissesCompletion(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPasteDismissesCompletion(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
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
