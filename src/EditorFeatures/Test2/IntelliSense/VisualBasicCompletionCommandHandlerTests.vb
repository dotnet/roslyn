' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Projection

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class VisualBasicCompletionCommandHandlerTests

        <WorkItem(546208)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MultiWordKeywordCommitBehavior()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class C
    Sub M()
        $$
    End Sub
End Class
                              </Document>)
                state.SendTypeChars("on")
                state.AssertSelectedCompletionItem("On Error GoTo", description:=String.Format(FeaturesResources.Keyword, "On Error GoTo") + vbCrLf + VBFeaturesResources.OnErrorGotoKeywordToolTip)
                state.SendTypeChars(" ")
                state.AssertSelectedCompletionItem("Error GoTo", description:=String.Format(FeaturesResources.Keyword, "Error GoTo") + vbCrLf + VBFeaturesResources.OnErrorGotoKeywordToolTip)
            End Using
        End Sub

        <WorkItem(546208)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MultiWordKeywordCommitBehavior2()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class C
    Sub M()
        $$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("next")
                state.AssertSelectedCompletionItem("On Error Resume Next", description:=String.Format(FeaturesResources.Keyword, "On Error Resume Next") + vbCrLf + VBFeaturesResources.OnErrorResumeNextKeywordToolTip)
                state.SendTypeChars(" ")
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionNotShownWhenBackspacingThroughWhitespace()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                  Module M
                                      Sub Foo()
                                          If True Then $$Console.WriteLine()
                                      End Sub
                                  End Module
                              </Document>)

                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion), WorkItem(541032)>
        Public Sub CompletionNotShownWhenBackspacingThroughNewline()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionAdjustInsertionText_CommitsOnOpenParens1()
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
                state.AssertCompletionSession()

                Assert.Contains("    FogBar(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionUpAfterDot()
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                state.AssertNoCompletionSession()
                state.SendTypeChars(".")
                state.AssertCompletionSession()
            End Using
        End Sub

        <WorkItem(546432)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionAdjustInsertionText_CommitsOnOpenParens2()
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
                state.AssertCompletionSession()

                Assert.Contains("    FogBar(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionDismissedAfterEscape1()
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                state.AssertNoCompletionSession()
                state.SendTypeChars(".")
                state.AssertCompletionSession()
                state.SendEscape()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(543497)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestEnterOnSoftSelection1()
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program.$$
                                    End Sub
                                End Class
                              </document>)

                state.SendInvokeCompletionList()
                state.AssertSelectedCompletionItem("Equals", isSoftSelected:=True)
                Dim caretPos = state.GetCaretPoint().BufferPosition.Position
                state.SendReturn()
                state.Workspace.Documents.First().GetTextView().Caret.MoveTo(New SnapshotPoint(state.Workspace.Documents.First().TextBuffer.CurrentSnapshot, caretPos))
                Assert.Contains("Program." + vbCrLf, state.GetLineFromCurrentCaretPosition().GetTextIncludingLineBreak(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionTestTab1()
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
                state.AssertNoCompletionSession()

                Assert.Contains("    FogBar", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DotIsInserted()
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Sub Main(args As String())
                                        $$
                                    End Sub
                                End Class
                              </document>)
                state.SendTypeChars("Progra.")
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem(displayText:="Equals", isSoftSelected:=True)
                Assert.Contains("Program.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestReturn1()
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
                state.AssertNoCompletionSession()
                Assert.Contains(<text>
    Sub Main(args As String())
        Program

    End Sub</text>.NormalizedValue, state.GetDocumentText(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestDown1()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
                state.SendDownKey()
                state.AssertSelectedCompletionItem(displayText:="B", isHardSelected:=True)
                state.SendDownKey()
                state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True)
                state.SendDownKey()
                state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True)
                state.SendPageUp()
                state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
                state.SendUpKey()
                state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestFirstCharacterDoesNotFilter1()
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
                state.AssertCompletionSession()
                Assert.Equal(3, state.CurrentCompletionPresenterSession.CompletionItems.Count)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestSecondCharacterDoesFilter1()
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
                state.WaitForAsynchronousOperations()
                Assert.Equal(4, state.CurrentCompletionPresenterSession.CompletionItems.Count)
                state.SendTypeChars("A")
                state.WaitForAsynchronousOperations()
                Assert.Equal(2, state.CurrentCompletionPresenterSession.CompletionItems.Count)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNavigateSoftToHard()
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program.$$
                                    End Sub
                                End Class
                              </document>)

                state.SendInvokeCompletionList()
                state.AssertSelectedCompletionItem(displayText:="Equals", isSoftSelected:=True)
                state.SendUpKey()
                state.AssertSelectedCompletionItem(displayText:="Equals", isHardSelected:=True)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestBackspaceBeforeCompletedComputation()
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

                state.AssertNoCompletionSession()
                state.SendTypeChars(".M")

                ' We should not have a session now.  Note: do not block as this will just hang things
                ' since the provider will not return.
                state.AssertNoCompletionSession(block:=False)

                ' Now, navigate back.
                state.SendBackspace()

                ' allow the provider to continue
                e.Set()

                ' At this point, completion will be available since the caret is still within the model's span.
                state.AssertCompletionSession()

                ' Now, navigate back again.  Completion should be dismissed
                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNavigationBeforeCompletedComputation()
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

                state.AssertNoCompletionSession()
                state.SendTypeChars(".Ma")

                ' We should not have a session now.  Note: do not block as this will just hang things
                ' since the provider will not return.
                state.AssertNoCompletionSession(block:=False)

                ' Now, navigate using the caret.
                state.SendMoveToPreviousCharacter()

                ' allow the provider to continue
                e.Set()

                ' We should not have a session since we tear things down if we see a caret move
                ' before the providers have returned.
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNavigateOutOfItemChangeSpan()
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                state.AssertNoCompletionSession()
                state.SendTypeChars(".Ma")
                state.AssertCompletionSession()
                state.SendMoveToPreviousCharacter()
                state.AssertCompletionSession()
                state.SendMoveToPreviousCharacter()
                state.AssertCompletionSession()
                state.SendMoveToPreviousCharacter()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestUndo1()
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                Class Program
                                    Shared Sub Main(args As String())
                                        Program$$
                                    End Sub
                                End Class
                              </document>)

                state.AssertNoCompletionSession()
                state.SendTypeChars(".Ma(")
                state.AssertCompletionSession()
                Assert.Contains(".Main(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendUndo()
                Assert.Contains(".Ma(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCommitAfterNavigation()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
                state.SendDownKey()
                state.AssertSelectedCompletionItem(displayText:="B", isHardSelected:=True)
                state.SendTab()
                Assert.Contains(".B", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestSelectCompletionItemThroughPresenter()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
                state.SendSelectCompletionItemThroughPresenterSession(state.CurrentCompletionPresenterSession.CompletionItems.First(
                                                           Function(i) i.DisplayText = "B"))
                state.SendTab()
                Assert.Contains(".B", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestFiltering1()
            Using state = TestState.CreateVisualBasicTestState(
                  <document>
Imports System

Class c
    Sub Main
        $$
    End Sub
End Class</document>)
                state.SendTypeChars("Sy")
                Assert.True(state.CompletionItemsContainsAll(displayText:={"OperatingSystem", "System"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"Exception", "Activator"}))
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestMSCorLibTypes()
            Using state = TestState.CreateVisualBasicTestState(
                  <document>
Imports System

Class c
    Inherits$$
End Class</document>)
                state.SendTypeChars(" ")
                state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll(displayText:={"Attribute", "Exception"}))
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestDescription1()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem(description:="Class TestException" & vbCrLf & "TestDoc")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestObjectCreationPreselection1()
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
                state.AssertSelectedCompletionItem(displayText:="List(Of Integer)", isHardSelected:=True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList(Of " & ChrW(&H2026) & ")", "List(Of " & ChrW(&H2026) & ")", "System"}))
                state.SendTypeChars("Li")
                state.AssertSelectedCompletionItem(displayText:="List(Of Integer)", isHardSelected:=True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList(Of " & ChrW(&H2026) & ")", "List(Of " & ChrW(&H2026) & ")"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"System"}))
                state.SendTypeChars("n")
                state.AssertSelectedCompletionItem(displayText:="LinkedList(Of " & ChrW(&H2026) & ")", isHardSelected:=True)
                state.SendBackspace()
                state.AssertSelectedCompletionItem(displayText:="List(Of Integer)", isHardSelected:=True)
                state.SendTab()
                Assert.Contains("New List(Of Integer)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(287, "https://github.com/dotnet/roslyn/issues/287")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotEnumPreselectionAfterBackspace()
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
                state.AssertSelectedCompletionItem(displayText:="b", isHardSelected:=True)
            End Using
        End Sub

        <WorkItem(543496)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNumericLiteralWithNoMatch()
            Using state = TestState.CreateVisualBasicTestState(
                  <Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i =$$
    End Sub
End Module</Document>)

                state.SendTypeChars(" 0")
                state.AssertNoCompletionSession()
                state.SendReturn()
                state.AssertNoCompletionSession()
                Assert.Equal(<Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i = 0

    End Sub
End Module</Document>.NormalizedValue, state.GetDocumentText())
            End Using
        End Sub

        <WorkItem(543496)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNumericLiteralWithPartialMatch()
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
                state.AssertNoCompletionSession()
                state.SendReturn()
                state.AssertNoCompletionSession()
                Assert.Equal(<Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i = 3

    End Sub
End Module</Document>.NormalizedValue, state.GetDocumentText())
            End Using
        End Sub

        <WorkItem(543496)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNumbersAfterLetters()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem(displayText:="Int32", isHardSelected:=True)
                state.SendReturn()
                state.AssertNoCompletionSession()
                Assert.Equal(<Document>
Imports System

Module Program
    Sub Main(args As String())
        Dim i = Int32

    End Sub
End Module</Document>.NormalizedValue, state.GetDocumentText())
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNotAfterTypingDotAfterIntegerLiteral()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub M()
        WriteLine(3$$
    end sub
end class
                              </Document>)

                state.SendTypeChars(".")
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestAfterExplicitInvokeAfterDotAfterIntegerLiteral()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub M()
        WriteLine(3.$$
    end sub
end class
                              </Document>)

                state.SendInvokeCompletionList()
                state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll({"ToString"}))
            End Using
        End Sub

        <WorkItem(543669)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestDeleteWordToLeft()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub M()
        $$
    end sub
end class
                              </Document>)
                state.SendTypeChars("Dim i =")
                state.AssertCompletionSession()
                state.SendDeleteWordToLeft()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(543617)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompletionGenericWithOpenParen()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub Foo(Of X)()
        $$
    end sub
end class
                              </Document>)
                state.SendTypeChars("Fo(")
                state.AssertCompletionSession()
                Assert.Equal("        Foo(", state.GetLineTextFromCaretPosition())
                Assert.DoesNotContain("Foo(Of", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(543617)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompletionGenericWithSpace()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
class c
    sub Foo(Of X)()
        $$
    end sub
end class
                              </Document>)
                state.SendTypeChars("Fo ")
                state.AssertCompletionSession()
                Assert.Equal("        Foo(Of ", state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitForImportsStatement1()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("Imports Sys")
                state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars("(")
                state.AssertNoCompletionSession()
                Assert.Contains("Imports Sys(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitForImportsStatement2()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("Imports Sys")
                state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(".")
                state.AssertCompletionSession()
                Assert.Contains("Imports System.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitForImportsStatement3()
            Using state = TestState.CreateVisualBasicTestState(
                            <Document>
                                $$
                            </Document>)

                state.SendTypeChars("Imports Sys")
                state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(" ")
                state.AssertNoCompletionSession()
                Assert.Contains("Imports Sys ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(544190)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotInsertEqualsForNamedParameterCommitWithColon()
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
                state.AssertNoCompletionSession()
                Assert.DoesNotContain(":=", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(544190)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoInsertEqualsForNamedParameterCommitWithSpace()
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
                state.AssertNoCompletionSession()
                Assert.Contains(":=", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(544150)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ConsumeHashForPreprocessorCompletion()
            Using state = TestState.CreateVisualBasicTestState(
                            <Document>
$$
                            </Document>)

                state.SendTypeChars("#re")
                state.SendTab()
                state.AssertNoCompletionSession()
                Assert.Equal("#Region", state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EnumCompletionTriggeredOnSpace()
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
                state.AssertSelectedCompletionItem(displayText:="Numeros.Dos", isSoftSelected:=True)
                Assert.Equal(1, state.CurrentCompletionPresenterSession.CompletionItems.Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Sub

        Private Function CreateTriggeredCompletionProvider(e As ManualResetEvent) As CompletionListProvider
            Return New MockCompletionProvider(getItems:=Function(t, p, c)
                                                            e.WaitOne()
                                                            Return Nothing
                                                        End Function,
                                              isTriggerCharacter:=Function(t, p) True)
        End Function

        <WorkItem(544297)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestVerbatimNamedIdentifierFiltering()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class Class1
    Private Sub Test([string] As String)
        Test($$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("s")
                state.AssertCompletionSession()
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "string:="))
                state.SendTypeChars("t")
                state.WaitForAsynchronousOperations()
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "string:="))
            End Using
        End Sub

        <WorkItem(544299)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestExclusiveNamedParameterCompletion()
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
                state.AssertCompletionSession()
                Assert.Equal(1, state.CurrentCompletionPresenterSession.CompletionItems.Count)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "str:="))
            End Using
        End Sub

        <WorkItem(544299)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestExclusiveNamedParameterCompletion2()
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
                state.AssertCompletionSession()
                Assert.Equal(3, state.CurrentCompletionPresenterSession.CompletionItems.Count)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "b:="))
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "num:="))
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "str:="))
                Assert.False(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "dbl:="))
            End Using
        End Sub

        <WorkItem(544471)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestDontCrashOnEmptyParameterList()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
&lt;Obsolete()$$&gt;
                              </Document>)

                state.SendTypeChars(" ")
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(544628)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub OnlyMatchOnLowercaseIfPrefixWordMatch()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Module Program
    $$
End Module
                              </Document>)

                state.SendTypeChars("z")
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("#Const", isSoftSelected:=True)
            End Using
        End Sub

        <WorkItem(544989)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MyBaseFinalize()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class C
    Protected Overrides Sub Finalize()
        MyBase.Finalize$$
    End Sub
End Class
                              </Document>)

                state.SendTypeChars("(")
                state.AssertSignatureHelpSession()
                Assert.True(state.SignatureHelpItemsContainsAll({"Object.Finalize()"}))
            End Using
        End Sub

        <WorkItem(551117)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNamedParameterSortOrder()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("args", isHardSelected:=True)
                state.SendDownKey()
                state.AssertSelectedCompletionItem("args:=", isHardSelected:=True)
            End Using
        End Sub

        <WorkItem(546810)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestLineContinuationCharacter()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("_AppDomain", isHardSelected:=False)
            End Using
        End Sub

        <WorkItem(547287)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNumberDismissesCompletion()
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
                state.AssertCompletionSession()
                state.SendTypeChars(".")
                state.AssertNoCompletionSession()
                state.SendBackspace()
                state.SendBackspace()

                state.SendTypeChars("(")
                state.AssertCompletionSession()
                state.SendTypeChars("-")
                state.AssertNoCompletionSession()
                state.SendBackspace()
                state.SendBackspace()

                state.SendTypeChars("(")
                state.AssertCompletionSession()
                state.SendTypeChars("1")
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestProjections()
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
                state.AssertCompletionSession()
                state.SendTypeCharsToSpecificViewAndBuffer("a", view, subjectBuffer)
                state.AssertSelectedCompletionItem(displayText:="arg")

                Dim text = view.TextSnapshot.GetText()
                Dim projection = DirectCast(topProjectionBuffer.TextBuffer, IProjectionBuffer)
                Dim sourceSpans = projection.CurrentSnapshot.GetSourceSpans()

                ' unmap our source spans without changing the top buffer
                projection.ReplaceSpans(0, sourceSpans.Count, {text}, EditOptions.DefaultMinimalChange, editTag:=Nothing)

                ' Make sure completion updates even though are subject buffer is not connected.
                Dim editorOperations = state.GetService(Of IEditorOperationsFactoryService).GetEditorOperations(view)
                editorOperations.Backspace()
                editorOperations.InsertText("b")
                state.AssertSelectedCompletionItem(displayText:="bbb")

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
                state.AssertSelectedCompletionItem(displayText:="bbb")
                state.SendTypeCharsToSpecificViewAndBuffer("b", view, subjectBuffer)
                state.AssertSelectedCompletionItem(displayText:="bbb")

                ' verify we can commit even when unmapped
                projection.ReplaceSpans(0, projection.CurrentSnapshot.GetSourceSpans.Count, {projection.CurrentSnapshot.GetText()}, EditOptions.DefaultMinimalChange, editTag:=Nothing)
                state.SendCommitUniqueCompletionListItem()

                Assert.Contains(<text>
Imports System
Module Program
    Sub Main(arg As String)
        Dim bbb = 234
        Console.WriteLine(bbb
    End Sub
End Module          </text>.NormalizedValue, state.GetDocumentText(), StringComparison.Ordinal)

            End Using
        End Sub

        <WorkItem(622957)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestBangFiltersInDocComment()
            Using state = TestState.CreateVisualBasicTestState(
                  <Document><![CDATA[
''' $$
Public Class TestClass
End Class
]]></Document>)

                state.SendTypeChars("<")
                state.AssertCompletionSession()
                state.SendTypeChars("!")
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("!--")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionUpAfterBackSpacetoWord()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                  Public E$$
                              </Document>)

                state.SendBackspace()
                state.AssertCompletionSession()
                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCompletionAfterBackspaceInStringLiteral()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo()
                                    Dim z = "aa$$"
                                End Sub
                              </Document>)

                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionUpAfterDeleteDot()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo()
                                    Dim z = "a"
                                     z.$$ToString()
                                End Sub
                              </Document>)

                state.SendBackspace()
                state.AssertCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotCompletionUpAfterDeleteRParen()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo()
                                    "a".ToString()$$
                                End Sub
                              </Document>)

                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotCompletionUpAfterDeleteLParen()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo()
                                    "a".ToString($$
                                End Sub
                              </Document>)

                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotCompletionUpAfterDeleteComma()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo(x as Integer, y as Integer)
                                    Foo(1,$$)
                                End Sub
                              </Document>)

                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionAfterDeleteKeyword()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
                                Sub Foo(x as Integer, y as Integer)
                                    Foo(1,2)
                                End$$ Sub
                              </Document>)

                state.SendBackspace()
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("End", description:=String.Format(FeaturesResources.Keyword, "End") + vbCrLf + VBFeaturesResources.EndKeywordToolTip)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCompletionOnBackspaceAtBeginningOfFile()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub


        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionUpAfterLeftCurlyBrace()
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

                state.AssertNoCompletionSession()
                state.SendTypeChars("{")
                state.AssertCompletionSession()
            End Using
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionUpAfterLeftAngleBracket()
            Using state = TestState.CreateVisualBasicTestState(
                              <document>
                                $$
                                Module Program
                                    Sub Main(args As String())
                                    End Sub
                                End Module
                              </document>)

                state.AssertNoCompletionSession()
                state.SendTypeChars("<")
                state.AssertCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvokeCompletionDoesNotFilter()
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
                state.AssertSelectedCompletionItem("String")
                state.CompletionItemsContainsAll({"Integer", "G"})
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvokeCompletionSelectsWithoutRegardToCaretPosition()
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
                state.AssertSelectedCompletionItem("String")
                state.CompletionItemsContainsAll({"Integer", "G"})
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvokeCompletionBeforeWordDoesNotSelect()
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
                state.AssertSelectedCompletionItem("AccessViolationException")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BackspaceCompletionInvokedSelectedAndUnfiltered()
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
                state.AssertSelectedCompletionItem("String")
                state.CompletionItemsContainsAll({"Integer", "G"})
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ListDismissedIfNoMatches()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("String", isHardSelected:=True)
                state.SendTypeChars("gg")
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvokeCompletionComesUpEvenIfNoMatches()
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
                state.AssertCompletionSession()
            End Using
        End Sub

        <WorkItem(674422)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BackspaceInvokeCompletionComesUpEvenIfNoMatches()
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
                state.AssertCompletionSession()
                state.SendBackspace()
                state.AssertCompletionSession()
            End Using
        End Sub

        <WorkItem(674366)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BackspaceCompletionSelects()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("Integer")
            End Using
        End Sub

        <WorkItem(675555)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BackspaceCompletionNeverFilters()
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
                state.AssertCompletionSession()
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(c) c.DisplayText = "AccessViolationException"))
                state.SendBackspace()
                state.AssertCompletionSession()
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(c) c.DisplayText = "AccessViolationException"))
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TabAfterQuestionMarkInEmptyLine()
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
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        ?" + vbTab)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TabAfterTextFollowedByQuestionMark()
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
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        a")
            End Using
        End Sub

        <WorkItem(669942)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DistinguishItemsWithDifferentGlyphs()
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
                state.WaitForAsynchronousOperations()
                Assert.Equal(state.CurrentCompletionPresenterSession.CompletionItems.Count, 2)
            End Using
        End Sub

        <WorkItem(670149)>
        <Fact(), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TabAfterNullableFollowedByQuestionMark()
            Using state = TestState.CreateVisualBasicTestState(
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

        <WorkItem(672474)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestInvokeSnippetCommandDismissesCompletion()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("Imp")
                state.AssertCompletionSession()
                state.SendInsertSnippetCommand()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(672474)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestSurroundWithCommandDismissesCompletion()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("Imp")
                state.AssertCompletionSession()
                state.SendSurroundWithCommand()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(716117)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub XmlCompletionNotTriggeredOnBackspaceInText()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document><![CDATA[
''' <summary>
''' text$$
''' </summary>
Class G
    Dim a As Integer?
End Class]]></Document>)

                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(716117)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub XmlCompletionNotTriggeredOnBackspaceInTag()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document><![CDATA[
''' <summary$$>
''' text
''' </summary>
Class G
    Dim a As Integer?
End Class]]></Document>)

                state.SendBackspace()
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("summary")
            End Using
        End Sub

        <WorkItem(674415)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BackspacingLastCharacterDismisses()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("A")
                state.AssertCompletionSession()
                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(719977)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub HardSelectionWithBuilderAndOneExactMatch()
            Using state = TestState.CreateVisualBasicTestState(
<Document>Module M
    Public $$
End Module</Document>)

                state.SendTypeChars("sub")
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("Sub")
                Assert.True(state.CurrentCompletionPresenterSession.Builder IsNot Nothing)
            End Using
        End Sub

        <WorkItem(828603)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SoftSelectionWithBuilderAndNoExactMatch()
            Using state = TestState.CreateVisualBasicTestState(
<Document>Module M
    Public $$
End Module</Document>)

                state.SendTypeChars("prop")
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("Property", isSoftSelected:=True)
                Assert.True(state.CurrentCompletionPresenterSession.Builder IsNot Nothing)
            End Using
        End Sub

        <WorkItem(792569)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitOnEnter()
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
                Assert.Equal(expected, state.GetDocumentText())
            End Using
        End Sub

        <WorkItem(546208)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SelectKeywordFirst()
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
                state.AssertSelectedCompletionItem("GetType", VBFeaturesResources.GettypeFunction + vbCrLf +
                    ReturnsSystemTypeObject + vbCrLf +
                    $"GetType({Typename}) As Type")
            End Using
        End Sub

        <WorkItem(828392)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ConstructorFiltersAsNew()
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
                state.AssertSelectedCompletionItem("New", isHardSelected:=True)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoUnmentionableTypeInObjectCreation()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Public Class C
    Sub Foo()
        Dim a = new$$
    End Sub
End Class

                              </Document>)
                state.SendTypeChars(" ")
                state.AssertSelectedCompletionItem("AccessViolationException", isSoftSelected:=True)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FilterPreferEnum()
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
                state.AssertSelectedCompletionItem("E.Foo", isHardSelected:=True)
            End Using
        End Sub

        <WorkItem(883295)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InsertOfOnSpace()
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
                state.WaitForAsynchronousOperations()
                state.SendDownKey()
                state.SendTypeChars(" ")
                Assert.Equal("        Dim a as Task(Of ", state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WorkItem(883295)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotInsertOfOnTab()
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
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        Dim a as Task")
            End Using
        End Sub

        <WorkItem(899414)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInPartialMethodDeclaration()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompletionInLinkedFiles()
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
                state.AssertSelectedCompletionItem("Thing1")
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendTypeChars("Thi")
                state.AssertSelectedCompletionItem("Thing1")
            End Using
        End Sub

        <WorkItem(916452)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SoftSelectedWithNoFilterText()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Imports System
Class C
    Public Sub M(day As DayOfWeek)
        M$$
    End Sub
End Class</Document>)
                state.SendTypeChars("(")
                state.AssertCompletionSession()
                Assert.True(state.CurrentCompletionPresenterSession.IsSoftSelected)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EnumSortingOrder()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Imports System
Class C
    Public Sub M(day As DayOfWeek)
        M$$
    End Sub
End Class</Document>)
                state.SendTypeChars("(")
                state.AssertCompletionSession()
                ' DayOfWeek.Monday should  immediately follow DayOfWeek.Friday
                Dim friday = state.CurrentCompletionPresenterSession.CompletionItems.First(Function(i) i.DisplayText = "DayOfWeek.Friday")
                Dim monday = state.CurrentCompletionPresenterSession.CompletionItems.First(Function(i) i.DisplayText = "DayOfWeek.Monday")
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.IndexOf(friday) = state.CurrentCompletionPresenterSession.CompletionItems.IndexOf(monday) - 1)
            End Using
        End Sub

        <WorkItem(951726)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DismissUponSave()
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    $$
End Class]]></Document>)
                state.SendTypeChars("Su")
                state.AssertSelectedCompletionItem("Sub")
                state.SendSave()
                state.AssertNoCompletionSession(block:=True)
                state.AssertMatchesTextStartingAtLine(2, "    Su")
            End Using
        End Sub

        <WorkItem(969794)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DeleteCompletionInvokedSelectedAndUnfiltered()
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
                state.AssertSelectedCompletionItem("String")
            End Using
        End Sub

        <WorkItem(871755)>
        <WorkItem(954556)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FilterPrefixOnlyOnBackspace1()
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Public Re$$
End Class
            ]]></Document>)
                state.SendBackspace()
                state.AssertSelectedCompletionItem("ReadOnly")
                state.SendTypeChars("a")
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(969040)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BackspaceTriggerOnlyIfOptionEnabled()
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Imports System
Class G
    Public Re$$
End Class
            ]]></Document>)
                state.Workspace.Options = state.Workspace.Options.WithChangedOption(CompletionOptions.TriggerOnTyping, LanguageNames.VisualBasic, False)
                state.SendBackspace()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(957450)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordsForIntrinsicsDeduplicated()
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
                state.WaitForAsynchronousOperations()
                ' Should only have one item called 'Double' and it should have a keyword glyph
                Dim doubleItem = state.CurrentCompletionPresenterSession.CompletionItems.Single(Function(c) c.DisplayText = "Double")
                Assert.True(doubleItem.Glyph.Value = Glyph.Keyword)
            End Using
        End Sub

        <WorkItem(957450)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordDeduplicationLeavesEscapedIdentifiers()
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
                state.WaitForAsynchronousOperations()
                ' We should have gotten the item corresponding to [Double] and the item for the Double keyword
                Dim doubleItems = state.CurrentCompletionPresenterSession.CompletionItems.Where(Function(c) c.DisplayText = "Double")
                Assert.Equal(2, doubleItems.Count())
                Assert.True(doubleItems.Any(Function(c) c.Glyph.Value = Glyph.Keyword))
                Assert.True(doubleItems.Any(Function(c) c.Glyph.Value = Glyph.ClassInternal))
            End Using
        End Sub

        <WorkItem(1075298)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitOnQuestionMarkForConditionalAccess()
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
                state.WaitForAsynchronousOperations()
                state.AssertMatchesTextStartingAtLine(4, "Dim x = String.Empty?")
            End Using
        End Sub

        <WorkItem(1659, "https://github.com/dotnet/roslyn/issues/1659")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DismissOnSelectAllCommand()
            Using state = TestState.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    Sub foo()
        $$]]></Document>)
                ' Note: the caret is at the file, so the Select All command's movement
                ' of the caret to the end of the selection isn't responsible for 
                ' dismissing the session.
                state.SendInvokeCompletionList()
                state.AssertCompletionSession()
                state.SendSelectAll()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(3088, "https://github.com/dotnet/roslyn/issues/3088")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotPreferParameterNames()
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
                state.AssertSelectedCompletionItem("Table")
            End Using
        End Sub
    End Class
End Namespace
