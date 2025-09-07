' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    <[UseExportProvider]>
    Public Class VisualBasicSnippetCommandHandlerTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabAtEndOfWord_NoActiveSession_ExpansionInserted()
            Dim markup = "Public Class$$ Goo"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SnippetExpansionClient.TryInsertExpansionReturnValue = True
                testState.SendTab()

                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal(New Span(7, 5), testState.SnippetExpansionClient.InsertExpansionSpan)
                Assert.Equal("Public Class Goo", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabAtEndOfWord_NoActiveSession_ExpansionNotInsertedCausesInsertedTab()
            Dim markup = "Class$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("Class    ", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabAtEndOfWord_ActiveSessionDoesNotCauseCauseAnotherExpansion()
            Dim markup = "Class$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, startActiveSession:=True)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("Class", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabAtEndOfWord_ActiveSession()
            Dim markup = "Class$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, startActiveSession:=True)
            Using testState
                testState.SendTab()

                Assert.True(testState.SnippetExpansionClient.TryHandleTabCalled)
                Assert.Equal("Class", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInMiddleOfWordCreatesSession()
            Dim markup = "Cla$$ss"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("Cla    ss", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInWhiteSpaceDoesNotCreateSession()
            Dim markup = "Class $$ Goo"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("Class      Goo", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabWithSelectionDoesNotCreateSession()
            Dim markup = <Markup><![CDATA[Class SomeClass
    {|Selection:Sub Goo
    End Sub$$|}
End Class
]]></Markup>.Value

            Dim expectedResults = <Markup><![CDATA[Class SomeClass
        
End Class
]]></Markup>.Value.Replace(vbLf, vbCrLf)

            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal(expectedResults, testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabWithSelectionOnWhitespaceQuestionMarkDoesNotCreateSession()
            Dim markup = <Markup><![CDATA[Class SomeClass
    {|Selection:Sub Goo
    End Sub
    ?$$|}
End Class
]]></Markup>.Value

            Dim expectedResults = <Markup><![CDATA[Class SomeClass
        
End Class
]]></Markup>.Value.Replace(vbLf, vbCrLf)

            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.Equal(expectedResults, testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_BackTab_ActiveSession()
            Dim markup = <Markup><![CDATA[
    $$Class Goo
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, startActiveSession:=True)
            Using testState
                testState.SendBackTab()

                Assert.True(testState.SnippetExpansionClient.TryHandleBackTabCalled)

                Dim expectedText = <Code><![CDATA[
    Class Goo
End Class
]]></Code>.Value.Replace(vbLf, vbCrLf)

                Assert.Equal(expectedText, testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_BackTab_NoActiveSession()
            Dim markup = <Markup><![CDATA[
    $$Class Goo
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendBackTab()

                Assert.True(testState.SnippetExpansionClient.TryHandleBackTabCalled)

                Dim expectedText = <Code><![CDATA[
Class Goo
End Class
]]></Code>.Value.Replace(vbLf, vbCrLf)

                Assert.Equal(expectedText, testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_Return_ActiveSession()
            Dim markup = <Markup><![CDATA[
$$    Class Goo
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, startActiveSession:=True)
            Using testState
                testState.SendReturn()

                Assert.True(testState.SnippetExpansionClient.TryHandleReturnCalled)

                Dim expectedText = <Code><![CDATA[
    Class Goo
End Class
]]></Code>.Value.Replace(vbLf, vbCrLf)

                Assert.Equal(expectedText, testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_Return_NoActiveSession()
            Dim markup = <Markup><![CDATA[
$$    Class Goo
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendReturn()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)

                Dim expectedText = <Code><![CDATA[

    Class Goo
End Class
]]></Code>.Value.Replace(vbLf, vbCrLf)

                Assert.Equal(expectedText, testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_Escape_ActiveSession()
            Dim markup = <Markup><![CDATA[
$$    Class Goo
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, startActiveSession:=True)
            Using testState
                testState.SendEscape()

                Assert.True(testState.SnippetExpansionClient.TryHandleEscapeCalled)

                Dim expectedText = <Code><![CDATA[
    Class Goo
End Class
]]></Code>.Value.Replace(vbLf, vbCrLf)

                Assert.Equal(expectedText, testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_Escape_NoActiveSession()
            Dim markup = <Markup><![CDATA[
$$    Class Goo
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendEscape()

                Assert.True(testState.SnippetExpansionClient.TryHandleEscapeCalled)

                Dim expectedText = <Code><![CDATA[
EscapePassedThrough!    Class Goo
End Class
]]></Code>.Value.Replace(vbLf, vbCrLf)

                Assert.Equal(expectedText, testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInsideComment_NoExpansionInserted()
            Dim markup = <Markup><![CDATA[
Class C
    Sub M()
        ' If$$
    End Sub
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInsideString_NoExpansionInserted()
            Dim markup = <Markup><![CDATA[
Class C
    Sub M()
        Dim x = "What if$$ this fails?"
    End Sub
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInsideXmlLiteral1_NoExpansionInserted()
            Dim markup = <Markup><![CDATA[
Class C
    Sub M()
        Dim x = <If$$>Testing</If>
    End Sub
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInsideXmlLiteral2_NoExpansionInserted()
            Dim markup = <Markup><![CDATA[
Class C
    Sub M()
        Dim x = <a>If$$</a>
    End Sub
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetListOnQuestionMarkTabDeletesQuestionMark()
            Dim markup = <Markup><![CDATA[
Class C
    ?$$
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.Equal(testState.GetLineTextFromCaretPosition(), "    ")
                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetListOnQuestionMarkTabNotAfterNullable()
            Dim markup = <Markup><![CDATA[
Class C
    Dim x as Integer?$$
End Class
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.Equal(testState.GetLineTextFromCaretPosition(), "    Dim x as Integer?    ")
                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetListOnQuestionMarkTabAtBeginningOfFile()
            Dim markup = <Markup>?$$</Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.Equal(testState.GetDocumentText(), "")
                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets), Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub SnippetCommandHandler_Interactive_Tab()
            Dim markup = "for$$"
            Dim testState = SnippetTestState.CreateSubmissionTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SnippetExpansionClient.TryInsertExpansionReturnValue = True
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("for    ", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets), Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub SnippetCommandHandler_Interactive_InsertSnippetCommand()
            Dim markup = "for$$"

            Dim testState = SnippetTestState.CreateSubmissionTestState(markup, LanguageNames.VisualBasic)
            Using testState
                Dim handler = testState.SnippetCommandHandler
                Dim state = handler.GetCommandState(New InsertSnippetCommandArgs(testState.TextView, testState.SubjectBuffer))
                Assert.True(state.IsUnspecified)

                testState.SnippetExpansionClient.TryInsertExpansionReturnValue = True

                Assert.False(testState.SendInsertSnippetCommand(AddressOf handler.ExecuteCommand))

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("for", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub
    End Class
End Namespace
