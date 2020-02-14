﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Snippets
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Projection
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class VisualBasicCompletionCommandHandlerTests

        <WorkItem(546208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546208")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MultiWordKeywordCommitBehavior() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Class C
    Sub M()
        $$
    End Sub
End Class
                              </Document>)
                state.SendTypeChars("on")
                Await state.AssertSelectedCompletionItem("On Error GoTo", description:=String.Format(FeaturesResources._0_Keyword, "On Error GoTo") + vbCrLf + VBFeaturesResources.Enables_the_error_handling_routine_that_starts_at_the_line_specified_in_the_line_argument_The_specified_line_must_be_in_the_same_procedure_as_the_On_Error_statement_On_Error_GoTo_bracket_label_0_1_bracket)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem("On Error GoTo", description:=String.Format(FeaturesResources._0_Keyword, "On Error GoTo") + vbCrLf + VBFeaturesResources.Enables_the_error_handling_routine_that_starts_at_the_line_specified_in_the_line_argument_The_specified_line_must_be_in_the_same_procedure_as_the_On_Error_statement_On_Error_GoTo_bracket_label_0_1_bracket)
            End Using
        End Function

        <WorkItem(546208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546208")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MultiWordKeywordCommitBehavior2() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Class C
    Sub M()
        $$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("next")
                Await state.AssertSelectedCompletionItem("On Error Resume Next", description:=String.Format(FeaturesResources._0_Keyword, "On Error Resume Next") + vbCrLf + VBFeaturesResources.When_a_run_time_error_occurs_execution_transfers_to_the_statement_following_the_statement_or_procedure_call_that_resulted_in_the_error)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionNotShownWhenBackspacingThroughWhitespace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                  Module M
                                      Sub Goo()
                                          If True Then $$Console.WriteLine()
                                      End Sub
                                  End Module
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion), WorkItem(541032, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541032")>
        Public Async Function CompletionNotShownWhenBackspacingThroughNewline() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Module Program
    Sub Main()
        If True And
$$False Then
         End If
    End Sub
End Module
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionAdjustInsertionText_CommitsOnOpenParens1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                  Module M
                                    Sub FogBar()
                                    End Sub
                                    Sub test()
                                      $$
                                    End Sub
                                  End Module
                              </document>)

                state.SendTypeChars("Fog(")
                Await state.AssertCompletionSession()

                Assert.Contains("    FogBar(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterDot() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(546432, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546432")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ImplementsCompletionFaultTolerance()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                  Imports System
                                  Class C
                                      Sub Goo() Implements ICloneable$$
                                  End Module
                              </Document>)

                state.SendTypeChars(".")
            End Using
        End Sub

        <WorkItem(5487, "https://github.com/dotnet/roslyn/issues/5487")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitCharTypedAtTheBeginingOfTheFilterSpan() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Fuction F() As Boolean
        If $$
    End Function
End Class
            ]]></Document>)

                state.SendTypeChars("tru")
                Await state.AssertCompletionSession()
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendLeftKey()
                Await state.AssertSelectedCompletionItem(isSoftSelected:=True)
                state.SendTypeChars("(")
                Assert.Equal("If (tru", state.GetLineTextFromCaretPosition().Trim())
                Assert.Equal("t", state.GetCaretPoint().BufferPosition.GetChar())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionAdjustInsertionText_CommitsOnOpenParens2() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                  Module M
                                    Sub FogBar(Of T)()
                                    End Sub
                                    Sub test()
                                      $$
                                    End Sub
                                  End Module
                              </document>)

                state.SendTypeChars("Fog(")
                Await state.AssertCompletionSession()
                Assert.Contains("    FogBar(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543497")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionDismissedAfterEscape1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                state.SendEscape()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(543497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543497")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEnterOnSoftSelection1() As Task
            ' Code must be left-aligned because of https://github.com/dotnet/roslyn/issues/27988
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
Class Program
    Shared Sub Main(args As String())
        Program.$$
    End Sub
End Class
                              </document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Equals", isSoftSelected:=True)
                Dim caretPos = state.GetCaretPoint().BufferPosition.Position
                state.SendReturn()
                state.Workspace.Documents.First().GetTextView().Caret.MoveTo(New SnapshotPoint(state.Workspace.Documents.First().GetTextBuffer().CurrentSnapshot, caretPos))
                Assert.Contains("Program." + vbCrLf, state.GetLineFromCurrentCaretPosition().GetTextIncludingLineBreak(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionTestTab1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                  Module M
                                    Sub FogBar()
                                    End Sub
                                    Sub test()
                                      $$
                                    End Sub
                                  End Module
                              </document>)

                state.SendTypeChars("Fog")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("    FogBar", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DotIsInserted() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Sub Main(args As String())
                                        $$
                                    End Sub
                                End Class
                              </document>)
                state.SendTypeChars("Progra.")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="Equals", isSoftSelected:=True)
                Assert.Contains("Program.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReturn1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
Class Program
    Sub Main(args As String())
        $$
    End Sub
End Class
                              </document>)
                state.SendTypeChars("Progra")
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Assert.Contains(<text>
    Sub Main(args As String())
        Program

    End Sub</text>.NormalizedValue, state.GetDocumentText(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDown1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
Namespace N
    Class A
    End Class
    Class B
    End Class
    Class C
    End Class
End Namespace
Class Program
    Sub Main(args As String())
        N$$
    End Sub
End Class
                              </document>)
                state.SendTypeChars(".A")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem(displayText:="B", isHardSelected:=True)
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True)
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True)
                state.SendPageUp()
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
                state.SendUpKey()
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFirstCharacterDoesNotFilter1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
Namespace N
    Class A
    End Class
    Class B
    End Class
    Class C
    End Class
End Namespace
Class Program
    Sub Main(args As String())
        N$$
    End Sub
End Class
                              </document>)
                state.SendTypeChars(".A")
                Await state.AssertCompletionSession()
                Assert.Equal(3, state.GetCompletionItems().Count)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSecondCharacterDoesFilter1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
Namespace N
    Class AAA
    End Class
    Class AAB
    End Class
    Class BB
    End Class
    Class CC
    End Class
End Namespace
Class Program
    Sub Main(args As String())
        N$$
    End Sub
End Class
                              </document>)
                state.SendTypeChars(".A")
                Await state.AssertCompletionSession()
                Assert.Equal(4, state.GetCompletionItems().Count)
                state.SendTypeChars("A")
                Await state.AssertCompletionSession()
                Assert.Equal(2, state.GetCompletionItems().Count)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNavigateSoftToHard() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program.$$
                                    End Sub
                                End Class
                              </document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="Equals", isSoftSelected:=True)
                state.SendUpKey()
                Await state.AssertSelectedCompletionItem(displayText:="Equals", isHardSelected:=True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceBeforeCompletedComputation() As Task
            ' Simulate a very slow completionImplementation provider.
            Dim e = New ManualResetEvent(False)
            Dim provider = CreateTriggeredCompletionProvider(e)

            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>, extraCompletionProviders:={provider})

                Await state.AssertNoCompletionSession()
                state.SendTypeChars(".M")

                ' We should not have a session now.  Note: do not block as this will just hang things
                ' since the provider will not return.
                state.AssertNoCompletionSessionWithNoBlock()

                ' Now, navigate back.
                state.SendBackspace()

                ' allow the provider to continue
                e.Set()

                ' At this point, completionImplementation will be available since the caret is still within the model's span.
                Await state.AssertCompletionSession()

                ' Now, navigate back again.  Completion should be dismissed
                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNavigationBeforeCompletedComputation() As Task
            ' Simulate a very slow completionImplementation provider.
            Dim e = New ManualResetEvent(False)
            Dim provider = CreateTriggeredCompletionProvider(e)

            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>, extraCompletionProviders:={provider})

                Await state.AssertNoCompletionSession()
                state.SendTypeChars(".Ma")

                ' We should not have a session now.  Note: do not block as this will just hang things
                ' since the provider will not return.
                state.AssertNoCompletionSessionWithNoBlock()

                ' Now, navigate using the caret.
                state.SendMoveToPreviousCharacter()

                ' allow the provider to continue
                e.Set()

                ' Async provider can handle keys pressed while waiting for providers.
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNavigationOutBeforeCompletedComputation() As Task
            ' Simulate a very slow completionImplementation provider.
            Dim e = New ManualResetEvent(False)
            Dim provider = CreateTriggeredCompletionProvider(e)

            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>, extraCompletionProviders:={provider})

                Await state.AssertNoCompletionSession()
                state.SendTypeChars(".Ma")

                ' We should not have a session now.  Note: do not block as this will just hang things
                ' since the provider will not return.
                state.AssertNoCompletionSessionWithNoBlock()

                ' Now, navigate using the caret.
                state.SendDownKey()

                ' allow the provider to continue
                e.Set()

                ' Caret was intended to be moved out of the span. 
                ' Therefore, we should cancel the completion And move the caret.
                Await state.AssertNoCompletionSession()
                Assert.Contains("    End Sub", state.GetLineFromCurrentCaretPosition().GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNavigateOutOfItemChangeSpan() As Task
            ' Code must be left-aligned because of https://github.com/dotnet/roslyn/issues/27988
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
Class Program
    Shared Sub Main(args As String())
        Program$$
    End Sub
End Class
                              </document>)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars(".Ma")
                Await state.AssertCompletionSession()
                state.SendMoveToPreviousCharacter()
                Await state.AssertCompletionSession()
                state.SendMoveToPreviousCharacter()
                Await state.AssertCompletionSession()
                state.SendMoveToPreviousCharacter()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestUndo1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars(".Ma(")
                Await state.AssertCompletionSession()
                Assert.Contains(".Main(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendUndo()
                Assert.Contains(".Ma(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitAfterNavigation() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
Namespace N
    Class A
    End Class
    Class B
    End Class
    Class C
    End Class
End Namespace
Class Program
    Sub Main(args As String())
        N$$
    End Sub
End Class
                              </document>)
                state.SendTypeChars(".A")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem(displayText:="B", isHardSelected:=True)
                state.SendTab()
                Assert.Contains(".B", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFiltering1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <document>
Imports System

Class c
    Sub Main
        $$
    End Sub
End Class</document>)
                state.SendTypeChars("Sy")
                Await state.AssertCompletionItemsContainAll("OperatingSystem", "System")
                Await state.AssertCompletionItemsDoNotContainAny("Exception", "Activator")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMSCorLibTypes() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <document>
Imports System

Class c
    Inherits$$
End Class</document>)
                state.SendTypeChars(" ")
                Await state.AssertCompletionItemsContainAll("Attribute", "Exception")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDescription1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <document>
                      <![CDATA[Imports System

''' <summary>
''' TestDoc
''' </summary>
Class TestException
    Inherits Exception
End Class

Class MyException
    Inherits $$
End Class]]></document>)
                state.SendTypeChars("TestEx")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:="Class TestException" & vbCrLf & "TestDoc")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectCreationPreselection1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim x As List(Of Integer) = New$$
    End Sub
End Module]]></Document>)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="List(Of Integer)", isHardSelected:=True)
                Await state.AssertCompletionItemsContainAll("LinkedList", "List", "System")
                state.SendTypeChars("Li")
                Await state.AssertSelectedCompletionItem(displayText:="List(Of Integer)", isHardSelected:=True)
                Await state.AssertCompletionItemsContainAll("LinkedList", "List")
                Await state.AssertCompletionItemsDoNotContainAny("System")
                state.SendTypeChars("n")
                Await state.AssertSelectedCompletionItem(displayText:="LinkedList", displayTextSuffix:="(Of " & ChrW(&H2026) & ")", isHardSelected:=True)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="List(Of Integer)", isHardSelected:=True)
                state.SendTab()
                Assert.Contains("New List(Of Integer)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(287, "https://github.com/dotnet/roslyn/issues/287")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)> Public Async Function NotEnumPreselectionAfterBackspace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Enum E
    Bat
End Enum
 
Class C
    Sub Test(param As E)
        Dim b As E
        Test(b.$$)
    End Sub
End Class]]></Document>)

                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="b", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(543496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543496")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNumericLiteralWithNoMatch() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i =$$
    End Sub
End Module</Document>)

                state.SendTypeChars(" 0")
                Await state.AssertNoCompletionSession()
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Assert.Equal(<Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i = 0

    End Sub
End Module</Document>.NormalizedValue, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(543496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543496")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNumericLiteralWithPartialMatch() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i =$$
    End Sub
End Module</Document>)

                ' Could match Int32
                ' kayleh 1/17/2013, but we decided to have #s always dismiss the list in bug 547287
                state.SendTypeChars(" 3")
                Await state.AssertNoCompletionSession()
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Assert.Equal(<Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i = 3

    End Sub
End Module</Document>.NormalizedValue, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(543496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543496")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNumbersAfterLetters() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i =$$
    End Sub
End Module</Document>)

                ' Could match Int32
                state.SendTypeChars(" I3")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="Int32", isHardSelected:=True)
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Assert.Equal(<Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i = Int32

    End Sub
End Module</Document>.NormalizedValue, state.GetDocumentText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAfterTypingDotAfterIntegerLiteral() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
class c
    sub M()
        WriteLine(3$$
    end sub
end class
                              </Document>)

                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterExplicitInvokeAfterDotAfterIntegerLiteral() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
class c
    sub M()
        WriteLine(3.$$
    end sub
end class
                              </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("ToString")
            End Using
        End Function

        <WorkItem(543669, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543669")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeleteWordToLeft() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
class c
    sub M()
        $$
    end sub
end class
                              </Document>)
                state.SendTypeChars("Dim i =")
                Await state.AssertCompletionSession()
                state.SendDeleteWordToLeft()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(543617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543617")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionGenericWithOpenParen() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
class c
    sub Goo(Of X)()
        $$
    end sub
end class
                              </Document>)
                state.SendTypeChars("Go(")
                Await state.AssertCompletionSession()
                Assert.Equal("        Goo(", state.GetLineTextFromCaretPosition())
                Assert.DoesNotContain("Goo(Of", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543617")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionGenericWithSpace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
class c
    sub Goo(Of X)()
        $$
    end sub
end class
                              </Document>)
                state.SendTypeChars("Go ")
                Await state.AssertCompletionSession()
                Assert.Equal("        Goo(Of ", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForImportsStatement1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("Imports Sys")
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars("(")
                Await state.AssertNoCompletionSession()
                Assert.Contains("Imports Sys(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForImportsStatement2() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("Imports Sys")
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                Assert.Contains("Imports System.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForImportsStatement3() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document>
                                $$
                            </Document>)

                state.SendTypeChars("Imports Sys")
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
                Assert.Contains("Imports Sys ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(544190, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544190")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoNotInsertEqualsForNamedParameterCommitWithColon() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document>
    Class Class1
        Sub Method()
            Test($$
        End Sub
        Sub Test(Optional x As Integer = 42)
 
        End Sub
    End Class 
                            </Document>)

                state.SendTypeChars("x:")
                Await state.AssertNoCompletionSession()
                Assert.DoesNotContain(":=", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(544190, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544190")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoInsertEqualsForNamedParameterCommitWithSpace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document>
    Class Class1
        Sub Method()
            Test($$
        End Sub
        Sub Test(Optional x As Integer = 42)
 
        End Sub
    End Class 
                            </Document>)

                state.SendTypeChars("x")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains(":=", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(544150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544150")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ConsumeHashForPreprocessorCompletion() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document>
$$
                            </Document>)

                state.SendTypeChars("#re")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("#Region", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnSpace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Enum Numeros
    Uno
    Dos
End Enum
Class Goo
    Sub Bar(a As Integer, n As Numeros)
    End Sub
    Sub Baz()
        Bar(0$$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars(", ")
                Await state.AssertSelectedCompletionItem(displayText:="Numeros.Dos", isSoftSelected:=True)
            End Using
        End Function

        Private Function CreateTriggeredCompletionProvider(e As ManualResetEvent) As CompletionProvider
            Return New MockCompletionProvider(getItems:=Function(t, p, c)
                                                            e.WaitOne()
                                                            Return Nothing
                                                        End Function,
                                              isTriggerCharacter:=Function(t, p) True)
        End Function

        <WorkItem(544297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544297")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVerbatimNamedIdentifierFiltering() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Class Class1
    Private Sub Test([string] As String)
        Test($$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("s")
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("string", ":=")
                state.SendTypeChars("t")
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("string", ":=")
            End Using
        End Function

        <WorkItem(544299, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544299")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExclusiveNamedParameterCompletion() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                      <Workspace>
                          <Project Language="Visual Basic" CommonReferences="true" LanguageVersion="VisualBasic15">
                              <Document>
Class Class1
    Private Sub Test()
        Goo(bool:=False,$$
    End Sub
 
    Private Sub Goo(str As String, character As Char)
    End Sub
 
    Private Sub Goo(str As String, bool As Boolean)
    End Sub
End Class
                              </Document>
                          </Project>
                      </Workspace>)

                state.SendTypeChars(" ")
                Await state.AssertCompletionSession()
                Assert.Equal(1, state.GetCompletionItems().Count)
                Await state.AssertCompletionItemsContain("str", ":=")
            End Using
        End Function

        <WorkItem(544299, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544299")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExclusiveNamedParameterCompletion2() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                      <Workspace>
                          <Project Language="Visual Basic" CommonReferences="true" LanguageVersion="VisualBasic15">
                              <Document>
Class Goo
    Private Sub Test()
        Dim m As Object = Nothing
        Method(obj:=m, $$
    End Sub

    Private Sub Method(obj As Object, num As Integer, str As String)
    End Sub
    Private Sub Method(dbl As Double, str As String)
    End Sub
    Private Sub Method(num As Integer, b As Boolean, str As String)
    End Sub
    Private Sub Method(obj As Object, b As Boolean, str As String)
    End Sub
End Class
                              </Document>
                          </Project>
                      </Workspace>)

                state.SendTypeChars(" ")
                Await state.AssertCompletionSession()
                Assert.Equal(3, state.GetCompletionItems().Count)
                Await state.AssertCompletionItemsContain("b", ":=")
                Await state.AssertCompletionItemsContain("num", ":=")
                Await state.AssertCompletionItemsContain("str", ":=")
                Assert.False(state.GetCompletionItems().Any(Function(i) i.DisplayText = "dbl" AndAlso i.DisplayTextSuffix = ":="))
            End Using
        End Function

        <WorkItem(544471, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544471")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDontCrashOnEmptyParameterList() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
&lt;Obsolete()$$&gt;
                              </Document>)

                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(544628, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544628")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyMatchOnLowercaseIfPrefixWordMatch() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Module Program
    $$
End Module
                              </Document>)

                state.SendTypeChars("z")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("#Const", isSoftSelected:=True)
            End Using
        End Function

        <WorkItem(544989, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544989")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MyBaseFinalize() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Class C
    Protected Overrides Sub Finalize()
        MyBase.Finalize$$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSignatureHelpItemsContainAll({"Object.Finalize()"})
            End Using
        End Function

        <WorkItem(551117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/551117")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedParameterSortOrder() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Imports System
Module Program
    Sub Main(args As String())
        Main($$
    End Sub
End Module
                              </Document>)

                state.SendTypeChars("a")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("args", isHardSelected:=True)
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem("args", displayTextSuffix:=":=", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(546810, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546810")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLineContinuationCharacter() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Imports System
Module Program
    Sub Main()
        Dim x = New $$
    End Sub
End Module
                              </Document>)

                state.SendTypeChars("_")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("_AppDomain", isHardSelected:=False)
            End Using
        End Function

        <WorkItem(547287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547287")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNumberDismissesCompletion() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Imports System
Module Program
    Sub Main()
        Console.WriteLine$$
    End Sub
End Module
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertCompletionSession()
                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()
                state.SendBackspace()
                state.SendBackspace()

                state.SendTypeChars("(")
                Await state.AssertCompletionSession()
                state.SendTypeChars("-")
                Await state.AssertNoCompletionSession()
                state.SendBackspace()
                state.SendBackspace()

                state.SendTypeChars("(")
                Await state.AssertCompletionSession()
                state.SendTypeChars("1")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/27446"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestProjections() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
{|S1:
Imports System
Module Program
    Sub Main(arg As String)
        Dim bbb = 234
        Console.WriteLine$$
    End Sub
End Module|}          </Document>)

                Dim subjectDocument = state.Workspace.Documents.First()
                Dim firstProjection = state.Workspace.CreateProjectionBufferDocument(
                    <Document>
{|S1:|}
{|S2: some text that's mapped to the surface buffer |}

                           </Document>.NormalizedValue, {subjectDocument}, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim topProjectionBuffer = state.Workspace.CreateProjectionBufferDocument(
                <Document>
{|S1:|}
{|S2:|}
                              </Document>.NormalizedValue, {firstProjection}, options:=ProjectionBufferOptions.WritableLiteralSpans)

                ' Test a view that has a subject buffer with multiple projection buffers in between
                Dim view = topProjectionBuffer.GetTextView()
                Dim subjectBuffer = subjectDocument.GetTextBuffer()

                state.SendTypeCharsToSpecificViewAndBuffer("(", view, subjectBuffer)
                Await state.AssertCompletionSession(view)
                state.SendTypeCharsToSpecificViewAndBuffer("a", view, subjectBuffer)
                Await state.AssertSelectedCompletionItem(displayText:="arg", projectionsView:=view)

                Dim text = view.TextSnapshot.GetText()
                Dim projection = DirectCast(topProjectionBuffer.GetTextBuffer(), IProjectionBuffer)
                Dim sourceSpans = projection.CurrentSnapshot.GetSourceSpans()

                ' unmap our source spans without changing the top buffer
                projection.ReplaceSpans(0, sourceSpans.Count, {subjectBuffer.CurrentSnapshot.CreateTrackingSpan(0, subjectBuffer.CurrentSnapshot.Length, SpanTrackingMode.EdgeInclusive)}, EditOptions.DefaultMinimalChange, editTag:=Nothing)

                state.SendBackspace()
                state.SendTypeChars("b")

                Await state.AssertSelectedCompletionItem(displayText:="bbb")

                ' prepare to remap our subject buffer
                Dim subjectBufferText = subjectDocument.GetTextBuffer().CurrentSnapshot.GetText()
                Using edit = subjectDocument.GetTextBuffer().CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber:=Nothing, editTag:=Nothing)
                    edit.Replace(New Span(0, subjectBufferText.Length), subjectBufferText.Replace("Console.WriteLine(a", "Console.WriteLine(b"))
                    edit.Apply()
                End Using

                Dim replacementSpans = sourceSpans.Select(Function(ss)
                                                              If ss.Snapshot.TextBuffer.ContentType.TypeName = "inert" Then
                                                                  Return DirectCast(ss.Snapshot.GetText(ss.Span), Object)
                                                              Else
                                                                  Return DirectCast(ss.Snapshot.CreateTrackingSpan(ss.Span, SpanTrackingMode.EdgeExclusive), Object)
                                                              End If
                                                          End Function).ToList()

                projection.ReplaceSpans(0, 1, replacementSpans, EditOptions.DefaultMinimalChange, editTag:=Nothing)

                ' the same completionImplementation session should still be active after the remapping.
                Await state.AssertSelectedCompletionItem(displayText:="bbb", projectionsView:=view)
                state.SendTypeCharsToSpecificViewAndBuffer("b", view, subjectBuffer)
                Await state.AssertSelectedCompletionItem(displayText:="bbb", projectionsView:=view)

                ' verify we can commit even when unmapped
                projection.ReplaceSpans(0, projection.CurrentSnapshot.GetSourceSpans.Count, {projection.CurrentSnapshot.GetText()}, EditOptions.DefaultMinimalChange, editTag:=Nothing)
                Await state.SendCommitUniqueCompletionListItemAsync()
                Assert.Contains(<text>
Imports System
Module Program
    Sub Main(arg As String)
        Dim bbb = 234
        Console.WriteLine(bbb
    End Sub
End Module          </text>.NormalizedValue, state.GetDocumentText(), StringComparison.Ordinal)

            End Using
        End Function

        <WorkItem(622957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622957")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBangFiltersInDocComment() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
''' $$
Public Class TestClass
End Class
]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("!")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("!--")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterBackSpacetoWord() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                  Public E$$
                              </Document>)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoCompletionAfterBackspaceInStringLiteral() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                Sub Goo()
                                    Dim z = "aa$$"
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterDeleteDot() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                Sub Goo()
                                    Dim z = "a"
                                     z.$$ToString()
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotCompletionUpAfterDeleteRParen() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                Sub Goo()
                                    "a".ToString()$$
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotCompletionUpAfterDeleteLParen() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                Sub Goo()
                                    "a".ToString($$
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotCompletionUpAfterDeleteComma() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                Sub Goo(x as Integer, y as Integer)
                                    Goo(1,$$)
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionAfterDeleteKeyword() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
                                Sub Goo(x as Integer, y as Integer)
                                    Goo(1,2)
                                End$$ Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("End", description:=String.Format(FeaturesResources._0_Keyword, "End") + vbCrLf + VBFeaturesResources.Stops_execution_immediately)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoCompletionOnBackspaceAtBeginningOfFile() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterLeftCurlyBrace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                Imports System
                                Imports System.Collections.Generic
                                Imports System.Linq

                                Module Program
                                    Sub Main(args As String())
                                        Dim l As New List(Of Integer) From $$
                                    End Sub
                                End Module
                              </document>)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars("{")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterLeftAngleBracket() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <document>
                                $$
                                Module Program
                                    Sub Main(args As String())
                                    End Sub
                                End Module
                              </document>)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionDoesNotFilter() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as String$$
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("String")
                Await state.AssertCompletionItemsContainAll("Integer", "G")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionSelectsWithoutRegardToCaretPosition() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as Str$$ing
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("String")
                Await state.AssertCompletionItemsContainAll("Integer", "G")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionBeforeWordDoesNotSelect() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as $$String
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("AccessViolationException")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceCompletionInvokedSelectedAndUnfiltered() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as String$$
    End Sub
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem("String")
                Await state.AssertCompletionItemsContainAll("Integer", "G")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ListDismissedIfNoMatches() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as $$
    End Sub
End Class
            ]]></Document>)
                state.SendTypeChars("str")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("String", isHardSelected:=True)
                state.SendTypeChars("gg")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionComesUpEvenIfNoMatches() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as gggg$$
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(674422, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674422")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceInvokeCompletionComesUpEvenIfNoMatches() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as gggg$$
    End Sub
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.AssertCompletionSession()
                state.SendBackspace()
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(674366, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674366")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceCompletionSelects() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as Integrr$$
    End Sub
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("Integer")
            End Using
        End Function

        <WorkItem(675555, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/675555")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceCompletionNeverFilters() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as String$$
    End Sub
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContainAll("AccessViolationException")
                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContainAll("AccessViolationException")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TabAfterQuestionMarkInEmptyLine()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        ?$$
    End Sub
End Class
            ]]></Document>)
                state.SendTab()
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        ?" + vbTab)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TabAfterTextFollowedByQuestionMark()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        a?$$
    End Sub
End Class
            ]]></Document>)
                state.SendTab()
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        a")
            End Using
        End Sub

        <WorkItem(669942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/669942")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DistinguishItemsWithDifferentGlyphs() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Imports System.Linq
Class Test
    Sub [Select]()
    End Sub
    Sub Goo()
        Dim k As Integer = 1
        $$
    End Sub
End Class

            ]]></Document>)
                state.SendTypeChars("selec")
                Await state.AssertCompletionSession()
                Assert.Equal(state.GetCompletionItems().Count, 2)
            End Using
        End Function

        <WorkItem(670149, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670149")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TabAfterNullableFollowedByQuestionMark()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Dim a As Integer?$$
End Class
            ]]></Document>)
                state.SendTab()
                Assert.Equal(state.GetLineTextFromCaretPosition(), "    Dim a As Integer?" + vbTab)
            End Using
        End Sub

        <WorkItem(672474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672474")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvokeSnippetCommandDismissesCompletion() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("Imp")
                Await state.AssertCompletionSession()
                state.SendInsertSnippetCommand()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(672474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672474")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSurroundWithCommandDismissesCompletion() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("Imp")
                Await state.AssertCompletionSession()
                state.SendSurroundWithCommand()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(716117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716117")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function XmlCompletionNotTriggeredOnBackspaceInText() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document><![CDATA[
''' <summary>
''' text$$
''' </summary>
Class G
    Dim a As Integer?
End Class]]></Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(716117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716117")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function XmlCompletionNotTriggeredOnBackspaceInTag() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document><![CDATA[
''' <summary$$>
''' text
''' </summary>
Class G
    Dim a As Integer?
End Class]]></Document>)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("summary")
            End Using
        End Function

        <WorkItem(674415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/674415")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspacingLastCharacterDismisses() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("A")
                Await state.AssertCompletionSession()
                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(719977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/719977")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function HardSelectionWithBuilderAndOneExactMatch() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
<Document>Module M
    Public $$
End Module</Document>)

                state.SendTypeChars("sub")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("Sub")
                Assert.True(state.HasSuggestedItem())
            End Using
        End Function

        <WorkItem(828603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828603")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SoftSelectionWithBuilderAndNoExactMatch() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
<Document>Module M
    Public $$
End Module</Document>)

                state.SendTypeChars("prop")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("Property", isSoftSelected:=True)
                Assert.True(state.HasSuggestedItem())
            End Using
        End Function

        ' The test verifies the CommitCommandHandler isolated behavior which does not add '()' after 'Main'.
        ' The integrated VS behavior for the case is to get 'Main()'.
        <WorkItem(792569, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792569")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitOnEnter()
            Dim expected = <Document>Module M
    Sub Main()
        Main

    End Sub
End Module</Document>.Value.Replace(vbLf, vbCrLf)

            Using state = TestStateFactory.CreateVisualBasicTestState(
<Document>Module M
    Sub Main()
        Ma$$i
    End Sub
End Module</Document>)

                state.SendInvokeCompletionList()
                state.SendReturn()
                Assert.Equal(expected, state.GetDocumentText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestEnterIsConsumedWithAfterFullyTypedWordOption_NotFullyTyped()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document>
Class Class1
    Sub Main(args As String())
        $$
    End Sub
End Class
</Document>)

                Dim workspace = state.Workspace
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(CompletionOptions.EnterKeyBehavior, LanguageNames.VisualBasic, EnterKeyRule.AfterFullyTypedWord)))
                state.SendTypeChars("System.TimeSpan.FromMin")
                state.SendReturn()
                Assert.Equal(<text>
Class Class1
    Sub Main(args As String())
        System.TimeSpan.FromMinutes
    End Sub
End Class
</text>.NormalizedValue, state.GetDocumentText())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestEnterIsConsumedWithAfterFullyTypedWordOption_FullyTyped()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document>
Class Class1
    Sub Main(args As String())
        $$
    End Sub
End Class
</Document>)

                Dim workspace = state.Workspace
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(CompletionOptions.EnterKeyBehavior, LanguageNames.VisualBasic, EnterKeyRule.AfterFullyTypedWord)))

                state.SendTypeChars("System.TimeSpan.FromMinutes")
                state.SendReturn()
                Assert.Equal(<text>
Class Class1
    Sub Main(args As String())
        System.TimeSpan.FromMinutes

    End Sub
End Class
</text>.NormalizedValue, state.GetDocumentText())
            End Using
        End Sub

        <WorkItem(546208, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546208")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SelectKeywordFirst() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Class C
    Sub M()
        $$
    End Sub

    Sub GetType()
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("GetType")
                Await state.AssertSelectedCompletionItem(
                    displayText:="GetType",
                    displayTextSuffix:=String.Empty,
                    description:=VBFeaturesResources.GetType_function + vbCrLf +
                    VBWorkspaceResources.Returns_a_System_Type_object_for_the_specified_type_name + vbCrLf +
                    $"GetType({VBWorkspaceResources.typeName}) As Type")
            End Using
        End Function

        <WorkItem(828392, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/828392")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ConstructorFiltersAsNew() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Public Class Base
Public Sub New(x As Integer)
End Sub
End Class
Public Class Derived
Inherits Base
Public Sub New(x As Integer)
MyBase.$$
End Sub
End Class

                              </Document>)

                state.SendTypeChars("New")
                Await state.AssertSelectedCompletionItem("New", isHardSelected:=True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoUnmentionableTypeInObjectCreation() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Public Class C
    Sub Goo()
        Dim a = new$$
    End Sub
End Class

                              </Document>)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem("AccessViolationException", isSoftSelected:=True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function FilterPreferEnum() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Enum E
    Goo
    Bar
End Enum

Class Goo
End Class

Public Class C
    Sub Goo()
        E e = $$
    End Sub
End Class</Document>)
                state.SendTypeChars("g")
                Await state.AssertSelectedCompletionItem("E.Goo", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(883295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883295")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InsertOfOnSpace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Imports System.Threading.Tasks
Public Class C
    Sub Goo()
        Dim a as $$
    End Sub
End Class

                              </Document>)
                state.SendTypeChars("Task")
                Await state.WaitForUIRenderedAsync()
                state.SendDownKey()
                state.SendTypeChars(" ")
                Assert.Equal("        Dim a as Task(Of ", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(883295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/883295")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotInsertOfOnTab()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Imports System.Threading.Tasks
Public Class C
    Sub Goo()
        Dim a as $$
    End Sub
End Class

                              </Document>)
                state.SendTypeChars("Task")
                state.SendTab()
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        Dim a as Task")
            End Using
        End Sub

        <WorkItem(899414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899414")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInPartialMethodDeclaration() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Module Module1
 
    Sub Main()
 
    End Sub
 
End Module
 
Public Class Class2
    Partial Private Sub PartialMethod(ByVal x As Integer)
        $$
    End Sub
End Class</Document>)
                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionInLinkedFiles() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj" PreprocessorSymbols="Thing2=True">
                        <Document FilePath="C.vb">
Class C
    Sub M()
        $$
    End Sub

#If Thing1 Then
    Sub Thing1()
    End Sub
#End If
#If Thing2 Then
    Sub Thing2()
    End Sub
#End If
End Class
                              </Document>
                    </Project>
                    <Project Language="Visual Basic" CommonReferences="true" PreprocessorSymbols="Thing1=True">
                        <Document IsLinkFile="true" LinkAssemblyName="VBProj" LinkFilePath="C.vb"/>
                    </Project>
                </Workspace>)

                Dim documents = state.Workspace.Documents
                Dim linkDocument = documents.Single(Function(d) d.IsLinkFile)

                state.SendTypeChars("Thi")
                Await state.AssertSelectedCompletionItem("Thing1")
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendTypeChars("Thi")
                Await state.AssertSelectedCompletionItem("Thing1")
            End Using
        End Function

        <WorkItem(916452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916452")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SoftSelectedWithNoFilterText() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Imports System
Class C
    Public Sub M(day As DayOfWeek)
        M$$
    End Sub
End Class</Document>)
                state.SendTypeChars("(")
                Await state.AssertCompletionSession()
                Assert.True(state.IsSoftSelected())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumSortingOrder() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Imports System
Class C
    Public Sub M(day As DayOfWeek)
        M$$
    End Sub
End Class</Document>)
                state.SendTypeChars("(")
                Await state.AssertCompletionSession()
                ' DayOfWeek.Monday should  immediately follow DayOfWeek.Friday
                Dim friday = state.GetCompletionItems().First(Function(i) i.DisplayText = "DayOfWeek.Friday")
                Dim monday = state.GetCompletionItems().First(Function(i) i.DisplayText = "DayOfWeek.Monday")
                Assert.True(state.GetCompletionItems().IndexOf(friday) = state.GetCompletionItems().IndexOf(monday) - 1)
            End Using
        End Function

        <WorkItem(951726, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951726")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissUponSave() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    $$
End Class]]></Document>)

                state.SendTypeChars("Su")
                Await state.AssertSelectedCompletionItem("Sub")
                state.SendSave()
                Await state.AssertNoCompletionSession()
                state.AssertMatchesTextStartingAtLine(2, "    Su")
            End Using
        End Function

        <WorkItem(969794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969794")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DeleteCompletionInvokedSelectedAndUnfiltered() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub goo()
        Dim x as Stri$$ng
    End Sub
End Class
            ]]></Document>)
                state.SendDelete()
                Await state.AssertSelectedCompletionItem("String")
            End Using
        End Function

        <WorkItem(871755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/871755")>
        <WorkItem(954556, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/954556")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function FilterPrefixOnlyOnBackspace1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Public Re$$
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem("ReadOnly")
                state.SendTypeChars("a")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(969040, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/969040")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceTriggerOnlyIfOptionEnabled() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Public Re$$
End Class
            ]]></Document>)

                Dim workspace = state.Workspace
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(CompletionOptions.TriggerOnTyping, LanguageNames.VisualBasic, False)))
                state.SendBackspace()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(957450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/957450")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function KeywordsForIntrinsicsDeduplicated() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub Goo()
        $$
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                ' Should only have one item called 'Double' and it should have a keyword glyph
                Dim doubleItem = state.GetCompletionItems().Single(Function(c) c.DisplayText = "Double")
                Assert.True(doubleItem.Tags.Contains(WellKnownTags.Keyword))
            End Using
        End Function

        <WorkItem(957450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/957450")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function KeywordDeduplicationLeavesEscapedIdentifiers() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class [Double]
    Sub Goo()
        Dim x as $$
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                ' We should have gotten the item corresponding to [Double] and the item for the Double keyword
                Dim doubleItems = state.GetCompletionItems().Where(Function(c) c.DisplayText = "Double")
                Assert.Equal(2, doubleItems.Count())
                Assert.True(doubleItems.Any(Function(c) c.Tags.Contains(WellKnownTags.Keyword)))
                Assert.True(doubleItems.Any(Function(c) c.Tags.Contains(WellKnownTags.Class) AndAlso c.Tags.Contains(WellKnownTags.Internal)))
            End Using
        End Function

        <WorkItem(957450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/957450")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEscapedItemCommittedWithCloseBracket() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class [Interface]
    Sub Goo()
        Dim x As $$
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("Interf]")
                state.AssertMatchesTextStartingAtLine(4, "Dim x As [Interface]")
            End Using
        End Function

        <WorkItem(1075298, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1075298")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitOnQuestionMarkForConditionalAccess()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub Goo()
        Dim x = String.$$
    End Sub
End Class
            ]]></Document>)
                state.SendTypeChars("emp?")
                state.AssertMatchesTextStartingAtLine(4, "Dim x = String.Empty?")
            End Using
        End Sub

        <WorkItem(1659, "https://github.com/dotnet/roslyn/issues/1659")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissOnSelectAllCommand() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    Sub goo()
        $$]]></Document>)
                ' Note: the caret is at the file, so the Select All command's movement
                ' of the caret to the end of the selection isn't responsible for 
                ' dismissing the session.
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendSelectAll()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(3088, "https://github.com/dotnet/roslyn/issues/3088")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoNotPreferParameterNames() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Module Program
    Sub Main(args As String())
        Dim Table As Integer
        goo(table$$)
    End Sub

    Sub goo(table As String)

    End Sub
End Module]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Table")
            End Using
        End Function

        <WorkItem(4892, "https://github.com/dotnet/roslyn/issues/4892")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BooleanPreselection1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x as boolean = $$
    End Sub
End Module]]></Document>)
                state.SendTypeChars("f")
                Await state.AssertSelectedCompletionItem("False")
                state.SendBackspace()
                state.SendTypeChars("t")
                Await state.AssertSelectedCompletionItem("True")
            End Using
        End Function

        <WorkItem(4892, "https://github.com/dotnet/roslyn/issues/4892")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BooleanPreselection2() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Module Program
    Sub Main(args As String())
        foot($$
    End Sub

    Sub foot(x as boolean)
    End Sub
End Module]]></Document>)
                state.SendTypeChars("f")
                Await state.AssertSelectedCompletionItem("False")
                state.SendBackspace()
                state.SendTypeChars("t")
                Await state.AssertSelectedCompletionItem("True")
            End Using
        End Function

        <WorkItem(4892, "https://github.com/dotnet/roslyn/issues/4892")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BooleanPreselection3() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[

Module Program
    Class F
    End Class
    Class T
    End Class

    Sub Main(args As String())
        $$
    End Sub
End Module]]></Document>)
                state.SendTypeChars("f")
                Await state.AssertSelectedCompletionItem("F")
                state.SendBackspace()
                state.SendTypeChars("t")
                Await state.AssertSelectedCompletionItem("T")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                           <Document><![CDATA[
Imports System.Threading
Module Program
    Sub Cancel(x As Integer, cancellationToken As CancellationToken)
        Cancel(x + 1, $$)
    End Sub
End Module]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("cancellationToken").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection2() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                           <Document><![CDATA[
Module Program
    Sub Main(args As String())
        Dim aaz As Integer
        args = $$
    End Sub
End Module]]></Document>)
                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem("args").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection3() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                           <Document><![CDATA[
Class D
End Class
Class Program

    Sub Main(string() args)
    
       Dim cw = 7
       Dim cx as D = new D()
       Dim  cx2 as D = $$
    End Sub
End Class
]]></Document>)
                state.SendTypeChars("c")
                Await state.AssertSelectedCompletionItem("cx", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionLocalsOverType() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                           <Document><![CDATA[
Class A 
End Class
Class Program

    Sub Main(string() args)
    
       Dim  cx = new A()
       Dim cx2 as A = $$
    End Sub
End Class
]]></Document>)
                state.SendTypeChars("c")
                Await state.AssertSelectedCompletionItem("cx", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/6942"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionConvertibility1() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                           <Document><![CDATA[
Mustinherit Class C 
End Class
Class D 
inherits C
End Class
Class Program
    Sub Main(string() args)
    
       Dim cx = new D()
       Dim cx2 as C = $$
    End Sub
End Class
]]></Document>)
                state.SendTypeChars("c")
                Await state.AssertSelectedCompletionItem("cx", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionParamsArray() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                           <Document><![CDATA[

Class Program

    Sub Main(string() args)
    
       Dim azc as integer
       M2(a$$
    End Sub
    Sub M2(params int() yx)  
    End Sub
End Class
 
]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("azc", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionSetterValue() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                           <Document><![CDATA[
Class Program
    Private Async As Integer
    Public Property NewProperty() As Integer
        Get
            Return Async
        End Get
        Set(ByVal value As Integer)
            Async = $$
        End Set
    End Property
End Class]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("value", isHardSelected:=False, isSoftSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(12530, "https://github.com/dotnet/roslyn/issues/12530")>
        Public Async Function TestAnonymousTypeDescription() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                           <Document><![CDATA[
Imports System.Linq

Public Class Class1
    Sub Method()
        Dim x = {New With {.x = 1}}.ToArr$$
    End Sub
End Class
]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(description:=
$"<{ VBFeaturesResources.Extension }> Function IEnumerable(Of 'a).ToArray() As 'a()

{ FeaturesResources.Anonymous_Types_colon }
    'a { FeaturesResources.is_ } New With {{ .x As Integer }}")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(12530, "https://github.com/dotnet/roslyn/issues/12530")>
        Public Async Function TestAnonymousTypeDescription2() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                           <Document><![CDATA[
Imports System.Linq

Public Class Class1
    Sub Method()
        Dim x = {New With { Key .x = 1}}.ToArr$$
    End Sub
End Class
]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(description:=
$"<{ VBFeaturesResources.Extension }> Function IEnumerable(Of 'a).ToArray() As 'a()

{ FeaturesResources.Anonymous_Types_colon }
    'a { FeaturesResources.is_ } New With {{ Key .x As Integer }}")
            End Using
        End Function

        <WorkItem(11812, "https://github.com/dotnet/roslyn/issues/11812")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectCreationQualifiedName() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document><![CDATA[
 Class A
     Sub Test()
         Dim b As B.C(Of Integer) = New$$
     End Sub
 End Class
 
 Namespace B
     Class C(Of T)
     End Class
 End Namespace]]></Document>)

                state.SendTypeChars(" ")
                Await state.AssertCompletionSession()
                state.SendTypeChars("(")
                state.AssertMatchesTextStartingAtLine(3, "Dim b As B.C(Of Integer) = New B.C(Of Integer)(")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNonTrailingNamedArgumentInVB15_3() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                 <Workspace>
                     <Project Language="Visual Basic" LanguageVersion="VisualBasic15_3" CommonReferences="true" AssemblyName="VBProj">
                         <Document FilePath="C.vb">
Class C
    Sub M()
        Dim better As Integer = 2
        M(a:=1, $$)
    End Sub
    Sub M(a As Integer, bar As Integer, c As Integer)
    End Sub
End Class
                         </Document>
                     </Project>
                 </Workspace>)

                state.SendTypeChars("b")
                Await state.AssertSelectedCompletionItem(displayText:="bar", displayTextSuffix:=":=", isHardSelected:=True)
                state.SendTypeChars("e")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact>
        Public Async Function TestNonTrailingNamedArgumentInVB15_5() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                 <Workspace>
                     <Project Language="Visual Basic" LanguageVersion="VisualBasic15_5" CommonReferences="true" AssemblyName="VBProj">
                         <Document FilePath="C.vb">
Class C
    Sub M()
        Dim better As Integer = 2
        M(a:=1, $$)
    End Sub
    Sub M(a As Integer, bar As Integer, c As Integer)
    End Sub
End Class
                         </Document>
                     </Project>
                 </Workspace>)

                state.SendTypeChars("bar")
                Await state.AssertSelectedCompletionItem(displayText:="bar", displayTextSuffix:=":=", isHardSelected:=True)
                state.SendBackspace()
                state.SendBackspace()
                state.SendTypeChars("et")
                Await state.AssertSelectedCompletionItem(displayText:="better", isHardSelected:=True)
                state.SendTypeChars(", ")
                Assert.Contains("M(a:=1, better,", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSymbolInTupleLiteral() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
        Dim t = ($$)
    End Sub
End Class
}]]></Document>)

                state.SendTypeChars("Go")
                Await state.AssertSelectedCompletionItem(displayText:="Goo", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(Go:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSymbolInTupleLiteralAfterComma() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
        Dim t = (1, $$)
    End Sub
End Class
]]></Document>)

                state.SendTypeChars("Go")
                Await state.AssertSelectedCompletionItem(displayText:="Goo", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(1, Go:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSnippetInTupleLiteral() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
        Dim t = ($$)
    End Sub
End Class
}]]></Document>,
                  extraExportedTypes:={GetType(MockSnippetInfoService), GetType(SnippetCompletionProvider), GetType(StubVsEditorAdaptersFactoryService)}.ToList())

                Dim workspace = state.Workspace
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(CompletionOptions.SnippetsBehavior, LanguageNames.VisualBasic, SnippetsRule.AlwaysInclude)))

                state.SendTypeChars("Shortcu")
                Await state.AssertSelectedCompletionItem(displayText:="Shortcut", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(Shortcu:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSnippetInTupleLiteralAfterComma() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
        Dim t = (1, $$)
    End Sub
End Class
}]]></Document>,
                  extraExportedTypes:={GetType(MockSnippetInfoService), GetType(SnippetCompletionProvider), GetType(StubVsEditorAdaptersFactoryService)}.ToList())

                Dim workspace = state.Workspace
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(CompletionOptions.SnippetsBehavior, LanguageNames.VisualBasic, SnippetsRule.AlwaysInclude)))

                state.SendTypeChars("Shortcu")
                Await state.AssertSelectedCompletionItem(displayText:="Shortcut", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(1, Shortcu:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSnippetsNotExclusiveWhenAlwaysShowing() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
        Dim x as Integer = 3
        Dim t = $$
    End Sub
End Class
}]]></Document>,
                  extraExportedTypes:={GetType(MockSnippetInfoService), GetType(SnippetCompletionProvider), GetType(StubVsEditorAdaptersFactoryService)}.ToList())

                Dim workspace = state.Workspace
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(CompletionOptions.SnippetsBehavior, LanguageNames.VisualBasic, SnippetsRule.AlwaysInclude)))

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("x", "Shortcut")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBuiltInTypesKeywordInTupleLiteral() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
        Dim t = ($$)
    End Sub
End Class
}]]></Document>)

                state.SendTypeChars("Intege")
                Await state.AssertSelectedCompletionItem(displayText:="Integer", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(Intege:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBuiltInTypesKeywordInTupleLiteralAfterComma() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
        Dim t = (1, $$)
    End Sub
End Class
}]]></Document>)

                state.SendTypeChars("Intege")
                Await state.AssertSelectedCompletionItem(displayText:="Integer", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(1, Intege:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFunctionKeywordInTupleLiteral() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
        Dim t = ($$)
    End Sub
End Class
}]]></Document>)

                state.SendTypeChars("Functio")
                Await state.AssertSelectedCompletionItem(displayText:="Function", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(Functio:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFunctionKeywordInTupleLiteralAfterComma() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
        Dim t = (1, $$)
    End Sub
End Class
}]]></Document>)
                state.SendTypeChars("Functio")
                Await state.AssertSelectedCompletionItem(displayText:="Function", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(1, Functio:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSymbolInTupleType() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo()
       Dim t As ($$)
    End Sub
End Class
]]></Document>)
                state.SendTypeChars("Integ")
                Await state.AssertSelectedCompletionItem(displayText:="Integer", isHardSelected:=True)
                state.SendTypeChars(",")
                Assert.Contains("(Integer,", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvocationExpression() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo(Alice As Integer)
       Goo($$)
    End Sub
End Class
]]></Document>)

                state.SendTypeChars("Alic")
                Await state.AssertSelectedCompletionItem(displayText:="Alice", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("Goo(Alice:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvocationExpressionAfterComma() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                  <Document><![CDATA[
Class C
    Public Sub Goo(Alice As Integer, Bob As Integer)
       Goo(1, $$)
    End Sub
End Class
]]></Document>)

                state.SendTypeChars("B")
                Await state.AssertSelectedCompletionItem(displayText:="Bob", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("Goo(1, Bob:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(13161, "https://github.com/dotnet/roslyn/issues/13161")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitGenericDoesNotInsertEllipsis()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document><![CDATA[
Interface Goo(Of T)
End Interface

Class Bar
    Implements $$
End Class]]></Document>)

                Dim unicodeEllipsis = ChrW(&H2026).ToString()
                state.SendTypeChars("Goo")
                state.SendTab()
                Assert.Equal("Implements Goo(Of", state.GetLineTextFromCaretPosition().Trim())
                Assert.DoesNotContain(unicodeEllipsis, state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WorkItem(13161, "https://github.com/dotnet/roslyn/issues/13161")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitGenericDoesNotInsertEllipsisCommitOnParen()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document><![CDATA[
Interface Goo(Of T)
End Interface

Class Bar
    Implements $$
End Class]]></Document>)

                Dim unicodeEllipsis = ChrW(&H2026).ToString()
                state.SendTypeChars("Goo(")
                Assert.Equal("Implements Goo(", state.GetLineTextFromCaretPosition().Trim())
                Assert.DoesNotContain(unicodeEllipsis, state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WorkItem(13161, "https://github.com/dotnet/roslyn/issues/13161")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitGenericItemDoesNotInsertEllipsisCommitOnTab()
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document><![CDATA[
Interface Goo(Of T)
End Interface

Class Bar
    Dim x as $$
End Class]]></Document>)

                Dim unicodeEllipsis = ChrW(&H2026).ToString()
                state.SendTypeChars("Goo")
                state.SendTab()
                Assert.Equal("Dim x as Goo(Of", state.GetLineTextFromCaretPosition().Trim())
                Assert.DoesNotContain(unicodeEllipsis, state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WorkItem(15011, "https://github.com/dotnet/roslyn/issues/15011")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SymbolAndObjectPreselectionUnification() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document><![CDATA[
Module Module1

    Sub Main()
        Dim x As ProcessStartInfo = New $$
    End Sub

End Module
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Dim psi = state.GetCompletionItems().Where(Function(i) i.DisplayText.Contains("ProcessStartInfo")).ToArray()
                Assert.Equal(1, psi.Length)
            End Using
        End Function

        <WorkItem(394863, "https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=394863&triage=true")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ImplementsClause() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document><![CDATA[
Partial Class TestClass
    Implements IComparable(Of TestClass)

    Public Function CompareTo(other As TestClass) As Integer Implements I$$

    End Function

End Class
]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTab()
                Assert.Contains("IComparable(Of TestClass)", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(18785, "https://github.com/dotnet/roslyn/issues/18785")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceSoftSelectionIfNotPrefixMatch() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                            <Document><![CDATA[
Class C
    Sub Do()
        Dim x = new System.Collections.Generic.List(Of String)()
        x.$$Add("stuff")
    End Sub
End Class
]]></Document>)

                state.SendBackspace()
                Await state.AssertSelectedCompletionItem("x", isSoftSelected:=True)
                state.SendTypeChars(".")
                Assert.Contains("x.Add", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(28767, "https://github.com/dotnet/roslyn/issues/28767")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionDoesNotRemoveBracketsOnEnum() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                       <Document>
                          Class C
                             Sub S
                                 [$$] 
                             End Sub
                         End Class
                       </Document>)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars("Enu")
                Await state.AssertSelectedCompletionItem(displayText:="Enum", isHardSelected:=True)
                state.SendTab()
                Assert.Contains("[Enum]", state.GetDocumentText(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(30097, "https://github.com/dotnet/roslyn/issues/30097")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMRUKeepsTwoRecentlyUsedItems() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Class C
    Public Sub Ma(m As Double)
    End Sub

    Public Sub Test()
        $$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("M(M(M(M(")
                Await state.AssertCompletionSession()
                Assert.Equal("        Ma(m:=(Ma(m:=(", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(36546, "https://github.com/dotnet/roslyn/issues/36546")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotDismissIfEmptyOnBackspaceIfStartedWithBackspace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Imports System

Class C
    Public Sub M()
        Console.W$$
    End Sub
End Class
                              </Document>)

                state.SendBackspace()
                Await state.AssertCompletionItemsContainAll("WriteLine")
            End Using
        End Function

        <WorkItem(36546, "https://github.com/dotnet/roslyn/issues/36546")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotDismissIfEmptyOnMultipleBackspaceIfStartedInvoke() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                              <Document>
Imports System

Class C
    Public Sub M()
        Console.Wr$$
    End Sub
End Class
</Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendBackspace()
                state.SendBackspace()
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround1() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class C
    Sub goo(x As Integer)
        String.$$
]]></Document>)
                    state.SendTypeChars("is")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("IsInterned")
                End Using
            End Using

        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround2() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class C
    Sub goo(x As Integer)
        String.$$]]></Document>)
                    state.SendTypeChars("ı")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem()
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround3() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class TARIFE

End Class
Class C
    Sub goo(x As Integer)
        Dim t As $$
]]></Document>)
                    state.SendTypeChars("tarif")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("TARIFE")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround4() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class IFADE

End Class
Class ifTest

End Class

Class C
    Sub goo(x As Integer)
        Dim ifade As IFADE
        $$]]></Document>)
                    state.SendTypeChars("if")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("If")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround5() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class İFADE

End Class
Class ifTest

End Class

Class C
    Sub goo(x As Integer)
        Dim ifade As İFADE
        $$
]]></Document>)
                    state.SendTypeChars("if")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("If")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround6() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class TARİFE

End Class
Class C
    Sub goo(x As Integer)
        Dim obj As $$
]]></Document>)
                    state.SendTypeChars("tarif")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("TARİFE")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround7() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class İFADE

End Class
Class ifTest

End Class

Class C
    Sub goo(x As Integer)
        Dim obj As $$
]]></Document>)
                    state.SendTypeChars("ifad")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("İFADE")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround8() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class IFADE

End Class
Class ifTest

End Class

Class C
    Sub goo(x As Integer)
        Dim obj As $$
]]></Document>)
                    state.SendTypeChars("ifad")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("IFADE")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround9() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class IFADE

End Class
Class ifTest

End Class

Class C
    Sub goo(x As Integer)
        Dim ifade_ As İFADE
        $$]]></Document>)
                    state.SendTypeChars("IF")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("If")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround10() As Task
            Using New CultureContext(New Globalization.CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateVisualBasicTestState(
                               <Document><![CDATA[
Class İFADE

End Class
Class ifTest

End Class

Class C
    Sub goo(x As Integer)
        Dim ifade As İFADE
        $$
]]></Document>)
                    state.SendTypeChars("IF")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("If")
                End Using
            End Using

        End Function

        <ExportLanguageService(GetType(ISnippetInfoService), LanguageNames.VisualBasic), System.Composition.Shared>
        Friend Class MockSnippetInfoService
            Implements ISnippetInfoService

            <ImportingConstructor>
            Public Sub New()
            End Sub

            Public Function GetSnippetsAsync_NonBlocking() As IEnumerable(Of SnippetInfo) Implements ISnippetInfoService.GetSnippetsIfAvailable
                Return SpecializedCollections.SingletonEnumerable(New SnippetInfo("Shortcut", "Title", "Description", "Path"))
            End Function

            Public Function ShouldFormatSnippet(snippetInfo As SnippetInfo) As Boolean Implements ISnippetInfoService.ShouldFormatSnippet
                Return False
            End Function

            Public Function SnippetShortcutExists_NonBlocking(shortcut As String) As Boolean Implements ISnippetInfoService.SnippetShortcutExists_NonBlocking
                Return shortcut = "Shortcut"
            End Function
        End Class
    End Class
End Namespace
