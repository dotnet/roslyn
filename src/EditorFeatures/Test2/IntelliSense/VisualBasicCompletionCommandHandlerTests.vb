' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Projection

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class VisualBasicCompletionCommandHandlerTests

        <WorkItem(546208)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MultiWordKeywordCommitBehavior() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class C
    Sub M()
        $$
    End Sub
End Class
                              </Document>)
                state.SendTypeChars("on")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("On Error GoTo", description:=String.Format(FeaturesResources.Keyword, "On Error GoTo") + vbCrLf + VBFeaturesResources.OnErrorGotoKeywordToolTip).ConfigureAwait(True)
                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Error GoTo", description:=String.Format(FeaturesResources.Keyword, "Error GoTo") + vbCrLf + VBFeaturesResources.OnErrorGotoKeywordToolTip).ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(546208)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MultiWordKeywordCommitBehavior2() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class C
    Sub M()
        $$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("next")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("On Error Resume Next", description:=String.Format(FeaturesResources.Keyword, "On Error Resume Next") + vbCrLf + VBFeaturesResources.OnErrorResumeNextKeywordToolTip).ConfigureAwait(True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionNotShownWhenBackspacingThroughWhitespace() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                  Module M
                                      Sub Foo()
                                          If True Then $$Console.WriteLine()
                                      End Sub
                                  End Module
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion), WorkItem(541032)>
        Public Async Function CompletionNotShownWhenBackspacingThroughNewline() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionAdjustInsertionText_CommitsOnOpenParens1() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertCompletionSession().ConfigureAwait(True)

                Assert.Contains("    FogBar(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterDot() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars(".")
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(546432)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ImplementsCompletionFaultTolerance()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                  Imports System
                                  Class C
                                      Sub Foo() Implements ICloneable$$
                                  End Module
                              </Document>)

                state.SendTypeChars(".")
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionAdjustInsertionText_CommitsOnOpenParens2() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertCompletionSession().ConfigureAwait(True)

                Assert.Contains("    FogBar(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionDismissedAfterEscape1() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars(".")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendEscape()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(543497)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEnterOnSoftSelection1() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program.$$
                                    End Sub
                                End Class
                              </document>)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Equals", isSoftSelected:=True).ConfigureAwait(True)
                Dim caretPos = state.GetCaretPoint().BufferPosition.Position
                state.SendReturn()
                state.Workspace.Documents.First().GetTextView().Caret.MoveTo(New SnapshotPoint(state.Workspace.Documents.First().TextBuffer.CurrentSnapshot, caretPos))
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Contains("Program." + vbCrLf, state.GetLineFromCurrentCaretPosition().GetTextIncludingLineBreak(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionTestTab1() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertNoCompletionSession().ConfigureAwait(True)

                Assert.Contains("    FogBar", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DotIsInserted() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Sub Main(args As String())
                                        $$
                                    End Sub
                                End Class
                              </document>)
                state.SendTypeChars("Progra.")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Equals", isSoftSelected:=True).ConfigureAwait(True)
                Assert.Contains("Program.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReturn1() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
Class Program
    Sub Main(args As String())
        $$
    End Sub
End Class
                              </document>)
                state.SendTypeChars("Progra")
                state.SendReturn()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains(<text>
    Sub Main(args As String())
        Program

    End Sub</text>.NormalizedValue, state.GetDocumentText(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDown1() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True).ConfigureAwait(True)
                state.SendDownKey()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="B", isHardSelected:=True).ConfigureAwait(True)
                state.SendDownKey()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True).ConfigureAwait(True)
                state.SendDownKey()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True).ConfigureAwait(True)
                state.SendPageUp()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True).ConfigureAwait(True)
                state.SendUpKey()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFirstCharacterDoesNotFilter1() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.Equal(3, state.CurrentCompletionPresenterSession.CompletionItems.Count)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSecondCharacterDoesFilter1() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(4, state.CurrentCompletionPresenterSession.CompletionItems.Count)
                state.SendTypeChars("A")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(2, state.CurrentCompletionPresenterSession.CompletionItems.Count)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNavigateSoftToHard() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program.$$
                                    End Sub
                                End Class
                              </document>)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Equals", isSoftSelected:=True).ConfigureAwait(True)
                state.SendUpKey()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Equals", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact(Skip:="xunit"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceBeforeCompletedComputation() As Task
            ' Simulate a very slow completion provider.
            Dim e = New ManualResetEvent(False)
            Dim provider = CreateTriggeredCompletionProvider(e)

            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>, extraCompletionProviders:={provider})

                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars(".M")

                ' We should not have a session now.  Note: do not block as this will just hang things
                ' since the provider will not return.
                Await state.AssertNoCompletionSession(block:=False).ConfigureAwait(True)

                ' Now, navigate back.
                state.SendBackspace()

                ' allow the provider to continue
                e.Set()

                ' At this point, completion will be available since the caret is still within the model's span.
                Await state.AssertCompletionSession().ConfigureAwait(True)

                ' Now, navigate back again.  Completion should be dismissed
                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact(Skip:="xunit wait"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNavigationBeforeCompletedComputation() As Task
            ' Simulate a very slow completion provider.
            Dim e = New ManualResetEvent(False)
            Dim provider = CreateTriggeredCompletionProvider(e)

            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>, extraCompletionProviders:={provider})

                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars(".Ma")

                ' We should not have a session now.  Note: do not block as this will just hang things
                ' since the provider will not return.
                Await state.AssertNoCompletionSession(block:=False).ConfigureAwait(True)

                ' Now, navigate using the caret.
                state.SendMoveToPreviousCharacter()

                ' allow the provider to continue
                e.Set()

                ' We should not have a session since we tear things down if we see a caret move
                ' before the providers have returned.
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNavigateOutOfItemChangeSpan() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars(".Ma")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendMoveToPreviousCharacter()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendMoveToPreviousCharacter()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendMoveToPreviousCharacter()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestUndo1() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars(".Ma(")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.Contains(".Main(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendUndo()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Contains(".Ma(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitAfterNavigation() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True).ConfigureAwait(True)
                state.SendDownKey()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="B", isHardSelected:=True).ConfigureAwait(True)
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Contains(".B", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectCompletionItemThroughPresenter() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True).ConfigureAwait(True)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.SendSelectCompletionItemThroughPresenterSession(state.CurrentCompletionPresenterSession.CompletionItems.First(
                                                           Function(i) i.DisplayText = "B"))
                state.SendTab()
                Assert.Contains(".B", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Sub TestFiltering1()
            Using state = TestState.CreateVisualBasicTestState(
                  <document>
Imports System

Class c
    Sub Main
        $$
    End Sub
End Class</document>)
                state.SendTypeChars("Sy")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"OperatingSystem", "System"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"Exception", "Activator"}))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMSCorLibTypes() As Task
            Using state = TestState.CreateVisualBasicTestState(
                  <document>
Imports System

Class c
    Inherits$$
End Class</document>)
                state.SendTypeChars(" ")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"Attribute", "Exception"}))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDescription1() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(description:="Class TestException" & vbCrLf & "TestDoc").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectCreationPreselection1() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="List(Of Integer)", isHardSelected:=True).ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList(Of " & ChrW(&H2026) & ")", "List(Of " & ChrW(&H2026) & ")", "System"}))
                state.SendTypeChars("Li")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="List(Of Integer)", isHardSelected:=True).ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList(Of " & ChrW(&H2026) & ")", "List(Of " & ChrW(&H2026) & ")"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"System"}))
                state.SendTypeChars("n")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="LinkedList(Of " & ChrW(&H2026) & ")", isHardSelected:=True).ConfigureAwait(True)
                state.SendBackspace()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="List(Of Integer)", isHardSelected:=True).ConfigureAwait(True)
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Contains("New List(Of Integer)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(287, "https://github.com/dotnet/roslyn/issues/287")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotEnumPreselectionAfterBackspace() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="b", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(543496)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNumericLiteralWithNoMatch() As Task
            Using state = TestState.CreateVisualBasicTestState(
                  <Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i =$$
    End Sub
End Module</Document>)

                state.SendTypeChars(" 0")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendReturn()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Equal(<Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i = 0

    End Sub
End Module</Document>.NormalizedValue, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(543496)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNumericLiteralWithPartialMatch() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendReturn()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Equal(<Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i = 3

    End Sub
End Module</Document>.NormalizedValue, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(543496)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNumbersAfterLetters() As Task
            Using state = TestState.CreateVisualBasicTestState(
                  <Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i =$$
    End Sub
End Module</Document>)

                ' Could match Int32
                state.SendTypeChars(" I3")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Int32", isHardSelected:=True).ConfigureAwait(True)
                state.SendReturn()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
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
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub M()
        WriteLine(3$$
    end sub
end class
                              </Document>)

                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterExplicitInvokeAfterDotAfterIntegerLiteral() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub M()
        WriteLine(3.$$
    end sub
end class
                              </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll({"ToString"}))
            End Using
        End Function

        <WorkItem(543669)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeleteWordToLeft() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub M()
        $$
    end sub
end class
                              </Document>)
                state.SendTypeChars("Dim i =")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendDeleteWordToLeft()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(543617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionGenericWithOpenParen() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub Foo(Of X)()
        $$
    end sub
end class
                              </Document>)
                state.SendTypeChars("Fo(")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.Equal("        Foo(", state.GetLineTextFromCaretPosition())
                Assert.DoesNotContain("Foo(Of", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543617)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionGenericWithSpace() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub Foo(Of X)()
        $$
    end sub
end class
                              </Document>)
                state.SendTypeChars("Fo ")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.Equal("        Foo(Of ", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForImportsStatement1() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("Imports Sys")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars("(")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("Imports Sys(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForImportsStatement2() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("Imports Sys")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars(".")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.Contains("Imports System.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForImportsStatement3() As Task
            Using state = TestState.CreateVisualBasicTestState(
                            <Document>
                                $$
                            </Document>)

                state.SendTypeChars("Imports Sys")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("Imports Sys ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(544190)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoNotInsertEqualsForNamedParameterCommitWithColon() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.DoesNotContain(":=", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(544190)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoInsertEqualsForNamedParameterCommitWithSpace() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains(":=", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(544150)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ConsumeHashForPreprocessorCompletion() As Task
            Using state = TestState.CreateVisualBasicTestState(
                            <Document>
$$
                            </Document>)

                state.SendTypeChars("#re")
                state.SendTab()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Equal("#Region", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnSpace() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Enum Numeros
    Uno
    Dos
End Enum
Class Foo
    Sub Bar(a As Integer, n As Numeros)
    End Sub
    Sub Baz()
        Bar(0$$
    End Sub
End Class
class Foo
                              </Document>)

                state.SendTypeChars(", ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Numeros.Dos", isSoftSelected:=True).ConfigureAwait(True)
                Assert.Equal(1, state.CurrentCompletionPresenterSession.CompletionItems.Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Function

        Private Function CreateTriggeredCompletionProvider(e As ManualResetEvent) As CompletionListProvider
            Return New MockCompletionProvider(getItems:=Function(t, p, c)
                                                            e.WaitOne()
                                                            Return Nothing
                                                        End Function,
                                              isTriggerCharacter:=Function(t, p) True)
        End Function

        <WorkItem(544297)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVerbatimNamedIdentifierFiltering() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class Class1
    Private Sub Test([string] As String)
        Test($$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("s")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "string:="))
                state.SendTypeChars("t")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "string:="))
            End Using
        End Function

        <WorkItem(544299)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExclusiveNamedParameterCompletion() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class Class1
    Private Sub Test()
        Foo(bool:=False,$$
    End Sub
 
    Private Sub Foo(str As String, character As Char)
    End Sub
 
    Private Sub Foo(str As String, bool As Boolean)
    End Sub
End Class
                              </Document>)

                state.SendTypeChars(" ")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.Equal(1, state.CurrentCompletionPresenterSession.CompletionItems.Count)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "str:="))
            End Using
        End Function

        <WorkItem(544299)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExclusiveNamedParameterCompletion2() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class Foo
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
                              </Document>)

                state.SendTypeChars(" ")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.Equal(3, state.CurrentCompletionPresenterSession.CompletionItems.Count)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "b:="))
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "num:="))
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "str:="))
                Assert.False(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "dbl:="))
            End Using
        End Function

        <WorkItem(544471)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDontCrashOnEmptyParameterList() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
&lt;Obsolete()$$&gt;
                              </Document>)

                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(544628)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function OnlyMatchOnLowercaseIfPrefixWordMatch() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Module Program
    $$
End Module
                              </Document>)

                state.SendTypeChars("z")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("#Const", isSoftSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(544989)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MyBaseFinalize() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class C
    Protected Overrides Sub Finalize()
        MyBase.Finalize$$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.True(state.SignatureHelpItemsContainsAll({"Object.Finalize()"}))
            End Using
        End Function

        <WorkItem(551117)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedParameterSortOrder() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Imports System
Module Program
    Sub Main(args As String())
        Main($$
    End Sub
End Module
                              </Document>)

                state.SendTypeChars("a")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("args", isHardSelected:=True).ConfigureAwait(True)
                state.SendDownKey()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("args:=", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(546810)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLineContinuationCharacter() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Imports System
Module Program
    Sub Main()
        Dim x = New $$
    End Sub
End Module
                              </Document>)

                state.SendTypeChars("_")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("_AppDomain", isHardSelected:=False).ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(547287)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNumberDismissesCompletion() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Imports System
Module Program
    Sub Main()
        Console.WriteLine$$
    End Sub
End Module
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendBackspace()
                state.SendBackspace()

                state.SendTypeChars("(")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTypeChars("-")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendBackspace()
                state.SendBackspace()

                state.SendTypeChars("(")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTypeChars("1")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestProjections() As Task
            Using state = TestState.CreateVisualBasicTestState(
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

                           </Document>.NormalizedValue, {subjectDocument}, LanguageNames.VisualBasic, options:=ProjectionBufferOptions.WritableLiteralSpans)

                Dim topProjectionBuffer = state.Workspace.CreateProjectionBufferDocument(
                <Document>
{|S1:|}
{|S2:|}
                              </Document>.NormalizedValue, {firstProjection}, LanguageNames.VisualBasic, options:=ProjectionBufferOptions.WritableLiteralSpans)

                ' Test a view that has a subject buffer with multiple projection buffers in between
                Dim view = topProjectionBuffer.GetTextView()
                Dim subjectBuffer = subjectDocument.GetTextBuffer()

                state.SendTypeCharsToSpecificViewAndBuffer("(", view, subjectBuffer)
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTypeCharsToSpecificViewAndBuffer("a", view, subjectBuffer)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="arg").ConfigureAwait(True)

                Dim text = view.TextSnapshot.GetText()
                Dim projection = DirectCast(topProjectionBuffer.TextBuffer, IProjectionBuffer)
                Dim sourceSpans = projection.CurrentSnapshot.GetSourceSpans()

                ' unmap our source spans without changing the top buffer
                projection.ReplaceSpans(0, sourceSpans.Count, {text}, EditOptions.DefaultMinimalChange, editTag:=Nothing)

                ' Make sure completion updates even though are subject buffer is not connected.
                Dim editorOperations = state.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)
                editorOperations.Backspace()
                editorOperations.InsertText("b")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="bbb").ConfigureAwait(True)

                ' prepare to remap our subject buffer
                Dim subjectBufferText = subjectDocument.TextBuffer.CurrentSnapshot.GetText()
                Using edit = subjectDocument.TextBuffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber:=Nothing, editTag:=Nothing)
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

                ' the same completion session should still be active after the remapping.
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="bbb").ConfigureAwait(True)
                state.SendTypeCharsToSpecificViewAndBuffer("b", view, subjectBuffer)
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="bbb").ConfigureAwait(True)

                ' verify we can commit even when unmapped
                projection.ReplaceSpans(0, projection.CurrentSnapshot.GetSourceSpans.Count, {projection.CurrentSnapshot.GetText()}, EditOptions.DefaultMinimalChange, editTag:=Nothing)
                state.SendCommitUniqueCompletionListItem()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)

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

        <WorkItem(622957)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBangFiltersInDocComment() As Task
            Using state = TestState.CreateVisualBasicTestState(
                  <Document><![CDATA[
''' $$
Public Class TestClass
End Class
]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTypeChars("!")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("!--").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterBackSpacetoWord() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                  Public E$$
                              </Document>)

                state.SendBackspace()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoCompletionAfterBackspaceInStringLiteral() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo()
                                    Dim z = "aa$$"
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterDeleteDot() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo()
                                    Dim z = "a"
                                     z.$$ToString()
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotCompletionUpAfterDeleteRParen() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo()
                                    "a".ToString()$$
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotCompletionUpAfterDeleteLParen() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo()
                                    "a".ToString($$
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotCompletionUpAfterDeleteComma() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo(x as Integer, y as Integer)
                                    Foo(1,$$)
                                End Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionAfterDeleteKeyword() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo(x as Integer, y as Integer)
                                    Foo(1,2)
                                End$$ Sub
                              </Document>)

                state.SendBackspace()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("End", description:=String.Format(FeaturesResources.Keyword, "End") + vbCrLf + VBFeaturesResources.EndKeywordToolTip).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoCompletionOnBackspaceAtBeginningOfFile() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function


        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterLeftCurlyBrace() As Task
            Using state = TestState.CreateVisualBasicTestState(
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

                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars("{")
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionUpAfterLeftAngleBracket() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                $$
                                Module Program
                                    Sub Main(args As String())
                                    End Sub
                                End Module
                              </document>)

                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars("<")
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionDoesNotFilter() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as String$$
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("String").ConfigureAwait(True)
                state.CompletionItemsContainsAll({"Integer", "G"})
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionSelectsWithoutRegardToCaretPosition() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as Str$$ing
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("String").ConfigureAwait(True)
                state.CompletionItemsContainsAll({"Integer", "G"})
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionBeforeWordDoesNotSelect() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as $$String
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("AccessViolationException").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceCompletionInvokedSelectedAndUnfiltered() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as String$$
    End Sub
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("String").ConfigureAwait(True)
                state.CompletionItemsContainsAll({"Integer", "G"})
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ListDismissedIfNoMatches() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as $$
    End Sub
End Class
            ]]></Document>)
                state.SendTypeChars("str")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("String", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars("gg")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionComesUpEvenIfNoMatches() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as gggg$$
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(674422)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceInvokeCompletionComesUpEvenIfNoMatches() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as gggg$$
    End Sub
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendBackspace()
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(674366)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceCompletionSelects() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as Integrr$$
    End Sub
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Integer").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(675555)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceCompletionNeverFilters() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as String$$
    End Sub
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(c) c.DisplayText = "AccessViolationException"))
                state.SendBackspace()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(c) c.DisplayText = "AccessViolationException"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TabAfterQuestionMarkInEmptyLine() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        ?$$
    End Sub
End Class
            ]]></Document>)
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        ?" + vbTab)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TabAfterTextFollowedByQuestionMark() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        a?$$
    End Sub
End Class
            ]]></Document>)
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        a")
            End Using
        End Function

        <WorkItem(669942)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DistinguishItemsWithDifferentGlyphs() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Imports System.Linq
Class Test
    Sub [Select]()
    End Sub
    Sub Foo()
        Dim k As Integer = 1
        $$
    End Sub
End Class

            ]]></Document>)
                state.SendTypeChars("selec")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(state.CurrentCompletionPresenterSession.CompletionItems.Count, 2)
            End Using
        End Function

        <WorkItem(670149)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TabAfterNullableFollowedByQuestionMark() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Dim a As Integer?$$
End Class
            ]]></Document>)
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(state.GetLineTextFromCaretPosition(), "    Dim a As Integer?" + vbTab)
            End Using
        End Function

        <WorkItem(672474)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvokeSnippetCommandDismissesCompletion() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("Imp")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendInsertSnippetCommand()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(672474)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSurroundWithCommandDismissesCompletion() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("Imp")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendSurroundWithCommand()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(716117)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function XmlCompletionNotTriggeredOnBackspaceInText() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document><![CDATA[
''' <summary>
''' text$$
''' </summary>
Class G
    Dim a As Integer?
End Class]]></Document>)

                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(716117)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function XmlCompletionNotTriggeredOnBackspaceInTag() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document><![CDATA[
''' <summary$$>
''' text
''' </summary>
Class G
    Dim a As Integer?
End Class]]></Document>)

                state.SendBackspace()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("summary").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(674415)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspacingLastCharacterDismisses() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("A")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(719977)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function HardSelectionWithBuilderAndOneExactMatch() As Task
            Using state = TestState.CreateVisualBasicTestState(
<Document>Module M
    Public $$
End Module</Document>)

                state.SendTypeChars("sub")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Sub").ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.Builder IsNot Nothing)
            End Using
        End Function

        <WorkItem(828603)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SoftSelectionWithBuilderAndNoExactMatch() As Task
            Using state = TestState.CreateVisualBasicTestState(
<Document>Module M
    Public $$
End Module</Document>)

                state.SendTypeChars("prop")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Property", isSoftSelected:=True).ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.Builder IsNot Nothing)
            End Using
        End Function

        <WorkItem(792569)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitOnEnter() As Task
            Dim expected = <Document>Module M
    Sub Main()
        Main

    End Sub
End Module</Document>.Value.Replace(vbLf, vbCrLf)

            Using state = TestState.CreateVisualBasicTestState(
<Document>Module M
    Sub Main()
        Ma$$i
    End Sub
End Module</Document>)

                state.SendInvokeCompletionList()
                state.SendReturn()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(expected, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(546208)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SelectKeywordFirst() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("GetType", VBFeaturesResources.GettypeFunction + vbCrLf +
                    ReturnsSystemTypeObject + vbCrLf +
                    $"GetType({Typename}) As Type").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(828392)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ConstructorFiltersAsNew() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("New", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoUnmentionableTypeInObjectCreation() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Public Class C
    Sub Foo()
        Dim a = new$$
    End Sub
End Class

                              </Document>)
                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("AccessViolationException", isSoftSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function FilterPreferEnum() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Enum E
    Foo
    Bar
End Enum

Class Foo
End Class

Public Class C
    Sub Foo()
        E e = $$
    End Sub
End Class</Document>)
                state.SendTypeChars("f")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("E.Foo", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(883295)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InsertOfOnSpace() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Imports System.Threading.Tasks
Public Class C
    Sub Foo()
        Dim a as $$
    End Sub
End Class

                              </Document>)
                state.SendTypeChars("Task")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.SendDownKey()
                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal("        Dim a as Task(Of ", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(883295)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoNotInsertOfOnTab() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Imports System.Threading.Tasks
Public Class C
    Sub Foo()
        Dim a as $$
    End Sub
End Class

                              </Document>)
                state.SendTypeChars("Task")
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        Dim a as Task")
            End Using
        End Function

        <WorkItem(899414)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NotInPartialMethodDeclaration() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionInLinkedFiles() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Thing1").ConfigureAwait(True)
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendTypeChars("Thi")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Thing1").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(916452)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SoftSelectedWithNoFilterText() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Imports System
Class C
    Public Sub M(day As DayOfWeek)
        M$$
    End Sub
End Class</Document>)
                state.SendTypeChars("(")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.IsSoftSelected)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumSortingOrder() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Imports System
Class C
    Public Sub M(day As DayOfWeek)
        M$$
    End Sub
End Class</Document>)
                state.SendTypeChars("(")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                ' DayOfWeek.Monday should  immediately follow DayOfWeek.Friday
                Dim friday = state.CurrentCompletionPresenterSession.CompletionItems.First(Function(i) i.DisplayText = "DayOfWeek.Friday")
                Dim monday = state.CurrentCompletionPresenterSession.CompletionItems.First(Function(i) i.DisplayText = "DayOfWeek.Monday")
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.IndexOf(friday) = state.CurrentCompletionPresenterSession.CompletionItems.IndexOf(monday) - 1)
            End Using
        End Function

        <WorkItem(951726)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissUponSave() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    $$
End Class]]></Document>)
                state.SendTypeChars("Su")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Sub").ConfigureAwait(True)
                state.SendSave()
                Await state.AssertNoCompletionSession(block:=True).ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(2, "    Su")
            End Using
        End Function

        <WorkItem(969794)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DeleteCompletionInvokedSelectedAndUnfiltered() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub foo()
        Dim x as Stri$$ng
    End Sub
End Class
            ]]></Document>)
                state.SendDelete()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("String").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(871755)>
        <WorkItem(954556)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function FilterPrefixOnlyOnBackspace1() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Public Re$$
End Class
            ]]></Document>)
                state.SendBackspace()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("ReadOnly").ConfigureAwait(True)
                state.SendTypeChars("a")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(969040)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceTriggerOnlyIfOptionEnabled() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Public Re$$
End Class
            ]]></Document>)
                state.Workspace.Options = state.Workspace.Options.WithChangedOption(CompletionOptions.TriggerOnTyping, LanguageNames.VisualBasic, False)
                state.SendBackspace()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(957450)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function KeywordsForIntrinsicsDeduplicated() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub Foo()
        $$
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                ' Should only have one item called 'Double' and it should have a keyword glyph
                Dim doubleItem = state.CurrentCompletionPresenterSession.CompletionItems.Single(Function(c) c.DisplayText = "Double")
                Assert.True(doubleItem.Glyph.Value = Glyph.Keyword)
            End Using
        End Function

        <WorkItem(957450)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function KeywordDeduplicationLeavesEscapedIdentifiers() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class [Double]
    Sub Foo()
        Dim x as $$
    End Sub
End Class
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                ' We should have gotten the item corresponding to [Double] and the item for the Double keyword
                Dim doubleItems = state.CurrentCompletionPresenterSession.CompletionItems.Where(Function(c) c.DisplayText = "Double")
                Assert.Equal(2, doubleItems.Count())
                Assert.True(doubleItems.Any(Function(c) c.Glyph.Value = Glyph.Keyword))
                Assert.True(doubleItems.Any(Function(c) c.Glyph.Value = Glyph.ClassInternal))
            End Using
        End Function

        <WorkItem(1075298)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitOnQuestionMarkForConditionalAccess() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Sub Foo()
        Dim x = String.$$
    End Sub
End Class
            ]]></Document>)
                state.SendTypeChars("emp?")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(4, "Dim x = String.Empty?")
            End Using
        End Function

        <WorkItem(1659, "https://github.com/dotnet/roslyn/issues/1659")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissOnSelectAllCommand() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    Sub foo()
        $$]]></Document>)
                ' Note: the caret is at the file, so the Select All command's movement
                ' of the caret to the end of the selection isn't responsible for 
                ' dismissing the session.
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendSelectAll()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(3088, "https://github.com/dotnet/roslyn/issues/3088")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoNotPreferParameterNames() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Module Program
    Sub Main(args As String())
        Dim Table As Integer
        foo(table$$)
    End Sub

    Sub foo(table As String)

    End Sub
End Module]]></Document>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Table").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(4892, "https://github.com/dotnet/roslyn/issues/4892")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BooleanPreselection1() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Module Program
    Sub Main(args As String())
        Dim x as boolean = $$
    End Sub
End Module]]></Document>)
                state.SendTypeChars("f")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("False").ConfigureAwait(True)
                state.SendBackspace()
                state.SendTypeChars("t")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("True").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(4892, "https://github.com/dotnet/roslyn/issues/4892")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BooleanPreselection2() As Task
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Module Program
    Sub Main(args As String())
        foo($$
    End Sub

    Sub foo (x as boolean)
    End Sub
End Module]]></Document>)
                state.SendTypeChars("f")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("False").ConfigureAwait(True)
                state.SendBackspace()
                state.SendTypeChars("t")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("True").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(4892, "https://github.com/dotnet/roslyn/issues/4892")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BooleanPreselection3() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("F").ConfigureAwait(True)
                state.SendBackspace()
                state.SendTypeChars("t")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("T").ConfigureAwait(True)
            End Using
        End Function
    End Class
End Namespace
