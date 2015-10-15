' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    Public Class CSharpSnippetCommandHandlerTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabAtEndOfWord_NoActiveSession_ExpansionInserted()
            Dim markup = "public class$$ Foo"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SnippetExpansionClient.TryInsertExpansionReturnValue = True
                testState.SendTab()

                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal(New Span(7, 5), testState.SnippetExpansionClient.InsertExpansionSpan)
                Assert.Equal("public class Foo", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabAtEndOfPreprocessor_NoActiveSession_ExpansionInserted()
            Dim markup = "#if$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SnippetExpansionClient.TryInsertExpansionReturnValue = True
                testState.SendTab()

                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal(New Span(0, 3), testState.SnippetExpansionClient.InsertExpansionSpan)
                Assert.Equal("#if", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabAtEndOfWord_NoActiveSession_ExpansionNotInsertedCausesInsertedTab()
            Dim markup = "class$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SnippetExpansionClient.TryInsertExpansionReturnValue = False
                testState.SendTab()

                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("class    ", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabAtEndOfWord_ActiveSession()
            Dim markup = "class$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp, startActiveSession:=True)
            Using testState
                testState.SendTab()

                Assert.True(testState.SnippetExpansionClient.TryHandleTabCalled)
                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("class", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInMiddleOfWordCreatesSession()
            Dim markup = "cla$$ss"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SendTab()

                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("cla    ss", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInWhiteSpaceDoesNotCreateSession()
            Dim markup = "class $$ Foo"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("class      Foo", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabWithSelectionDoesNotCreateSession()
            Dim markup = <Markup><![CDATA[class SomeClass
{
    {|Selection:if
    if$$|}
}]]></Markup>.Value

            Dim expectedResults = <Markup><![CDATA[class SomeClass
{
        
}]]></Markup>.Value.Replace(vbLf, vbCrLf)

            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal(expectedResults, testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_BackTab_ActiveSession()
            Dim markup = "    $$class Foo {}"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp, startActiveSession:=True)
            Using testState
                testState.SendBackTab()

                Assert.True(testState.SnippetExpansionClient.TryHandleBackTabCalled)
                Assert.Equal("    class Foo {}", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_BackTab_NoActiveSession()
            Dim markup = "    $$class Foo {}"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SendBackTab()

                Assert.Equal("class Foo {}", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_Return_ActiveSession()
            Dim markup = "$$    class Foo {}"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp, startActiveSession:=True)
            Using testState
                testState.SendReturn()
                Assert.Equal("    class Foo {}", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_Return_NoActiveSession()
            Dim markup = "$$    class Foo {}"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SendReturn()
                Assert.Equal(Environment.NewLine & "    class Foo {}", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_Escape_ActiveSession()
            Dim markup = "$$    class Foo {}"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp, startActiveSession:=True)
            Using testState
                testState.SendEscape()

                Assert.True(testState.SnippetExpansionClient.TryHandleEscapeCalled)
                Assert.Equal("    class Foo {}", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_Escape_NoActiveSession()
            Dim markup = "$$    class Foo {}"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SendEscape()

                Assert.Equal("EscapePassedThrough!    class Foo {}", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInsideComment_NoExpansionInserted()
            Dim markup = <Markup><![CDATA[class C
{
    void M()
    {
        // class$$
    }
}
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Sub SnippetCommandHandler_TabInsideString_NoExpansionInserted()
            Dim markup = <Markup><![CDATA[class C
{
    void M()
    {
        var x = "What if$$ this fails?";
    }
}
]]></Markup>.Value
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.CSharp)
            Using testState
                testState.SendTab()

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets), Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub SnippetCommandHandler_Interactive_Tab()
            Dim markup = "for$$"
            Dim testState = SnippetTestState.CreateSubmissionTestState(markup, LanguageNames.CSharp)
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

            Dim testState = SnippetTestState.CreateSubmissionTestState(markup, LanguageNames.CSharp)
            Using testState
                Dim delegatedToNext = False
                Dim nextHandler =
                    Function()
                        delegatedToNext = True
                        Return CommandState.Unavailable
                    End Function

                Dim handler = testState.SnippetCommandHandler
                Dim state = handler.GetCommandState(New Commands.InsertSnippetCommandArgs(testState.TextView, testState.SubjectBuffer), nextHandler)
                Assert.True(delegatedToNext)
                Assert.False(state.IsAvailable)

                testState.SnippetExpansionClient.TryInsertExpansionReturnValue = True

                delegatedToNext = False
                testState.SendInsertSnippetCommand(AddressOf handler.ExecuteCommand, nextHandler)
                Assert.True(delegatedToNext)

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("for", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets), Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub SnippetCommandHandler_Interactive_SurroundWithCommand()
            Dim markup = "for$$"

            Dim testState = SnippetTestState.CreateSubmissionTestState(markup, LanguageNames.CSharp)
            Using testState
                Dim delegatedToNext = False
                Dim nextHandler =
                    Function()
                        delegatedToNext = True
                        Return CommandState.Unavailable
                    End Function

                Dim handler = CType(testState.SnippetCommandHandler, CSharp.Snippets.SnippetCommandHandler)
                Dim state = handler.GetCommandState(New Commands.SurroundWithCommandArgs(testState.TextView, testState.SubjectBuffer), nextHandler)
                Assert.True(delegatedToNext)
                Assert.False(state.IsAvailable)

                testState.SnippetExpansionClient.TryInsertExpansionReturnValue = True

                delegatedToNext = False
                testState.SendSurroundWithCommand(AddressOf handler.ExecuteCommand, nextHandler)
                Assert.True(delegatedToNext)

                Assert.False(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("for", testState.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Sub
    End Class
End Namespace
