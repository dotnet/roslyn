' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class VisualBasicCompletionCommandHandlerTests_XmlDoc

        <WorkItem(623219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623219")>
        <WorkItem(746919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746919")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitParam() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <param$$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    ''' <param name=""bar""", "")
            End Using
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowTypingDoubleQuote() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <param$$
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars("""")

                ' ''' <param"$$
                Await state.AssertLineTextAroundCaret("    ''' <param""", "")
            End Using
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowTypingSpace() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <param$$
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(" ")

                ' ''' <param $$
                Await state.AssertLineTextAroundCaret("    ''' <param ", "")

                ' Because the item contains a <space>, the completion list should still be present with the same selection
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSeeCommitOnSpace() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' <summary>
    ''' <$$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendTypeChars(" ")

                ' ''' <see $$/>
                Await state.AssertLineTextAroundCaret("    ''' <see ", "/>")

                ' The completion list should now contain attribute suggestions
                Await state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll({"cref", "langword"}))
                Assert.False(state.CompletionItemsContainsAny({"see"}))
            End Using
        End Function

    End Class
End Namespace
