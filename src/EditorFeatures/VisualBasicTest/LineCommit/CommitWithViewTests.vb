' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.LineCommit
    Public Class CommitWithViewTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterTypingAndDownArrow() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>imports   $$
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("System")
                testData.EditorOperations.MoveLineDown(extendSelection:=False)

                Assert.Equal("Imports System" + vbCrLf, testData.Buffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestDontCrashOnPastingCarriageReturnContainingString() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>Module Module1

    Sub Main()
        $$
    End Sub

End Module

</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("f'x" + vbCr + """")
                testData.EditorOperations.MoveLineDown(extendSelection:=False)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(539305, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539305")>
        Public Async Function TestCommitAfterTypingAndUpArrowInLambdaFooter() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Module M
                                Function Main()
                                    Dim q = Sub()

                                            End$$ sub
                                End Function
                            End Module
                        </Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("    ")
                testData.EditorOperations.MoveLineUp(extendSelection:=False)

                Assert.Equal("End Sub", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(5).GetText().Trim())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(539469, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539469")>
        Public Async Function TestCommitAfterTypingAndUpArrowInLambdaFooter2() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M
    Function Main()
        Dim q = Sub()

                $$End Sub
    End Function
End Module
                        </Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("    ")
                testData.EditorOperations.MoveLineUp(extendSelection:=False)

                ' The text should snap back to what it originally was
                Dim originalText = testData.Workspace.Documents.Single().InitialTextSnapshot.GetLineFromLineNumber(5).GetText()
                Assert.Equal(originalText, testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(5).GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(539457, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539457")>
        Public Async Function TestCommitAfterTypingAndUpArrowIntoBlankLine() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
                            Module M
                                Function Main()

                                    $$
                                End Function
                            End Module
                        </Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("dim x=42")
                testData.EditorOperations.MoveLineUp(extendSelection:=False)

                Assert.Equal("Dim x = 42", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(4).GetText().Trim())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(539411, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539411")>
        Public Async Function TestCommitAfterTypingInTrivia() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M
End Module

$$</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("#const   foo=2.0d")
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)

                Assert.Equal("#Const foo = 2D", testData.Buffer.CurrentSnapshot.Lines.Last().GetText().Trim())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(539599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539599")>
        <WorkItem(631913, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/631913")>
        Public Async Function TestCommitAfterTypingInTrivia2() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M[|
    dim foo = 1 + _
        _$$
        3|]
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText(" ")
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)

                Assert.Equal("    Dim foo = 1 + _", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(2).GetText())
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(545355, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545355")>
        Public Async Function TestCommitAfterTypingAttributeOfType() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>[|
$$|]
Class Foo
End Class
                        </Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("<ComClass>")
                testData.EditorOperations.MoveLineDown(extendSelection:=False)

                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(545355, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545355")>
        Public Async Function TestCommitAfterTypingAttributeOfMethod() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class Foo[|
    $$|]
    Sub Bar()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("<ClsCompliant>")
                testData.EditorOperations.MoveLineDown(extendSelection:=False)

                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(545355, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545355")>
        Public Async Function TestCommitAfterTypingInMethodNameAndThenMovingToAttribute() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Class Foo[|
    <ClsCompilant>
    Sub $$Bar()
    End Sub|]
End Class
                        ]]></Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("Foo")
                testData.EditorOperations.MoveLineUp(extendSelection:=False)

                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestNoCommitDuringInlineRename() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document><![CDATA[
Class Foo[|
    <ClsCompilant>
    Sub $$Bar()
    End Sub|]
End Class
                        ]]></Document>
                    </Project>
                </Workspace>)

                testData.StartInlineRenameSession()
                testData.EditorOperations.InsertText("Foo")
                testData.EditorOperations.MoveLineUp(extendSelection:=False)

                testData.AssertHadCommit(False)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(539599, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539599")>
        Public Async Function TestCommitAfterLeavingStatementAfterLineContinuation() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Module1
    Sub Main()
        Dim f3 = Function()[|
                     Return $$Function(x As String) As String
                                Return ""
                            End Function|]
                 End Function
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("_")
                testData.CommandHandler.ExecuteCommand(New ReturnKeyCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertNewLine())

                ' So far we should have had no commit
                testData.AssertHadCommit(False)

                testData.EditorOperations.MoveLineDown(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(539318, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539318")>
        Public Async Function TestCommitAfterDeletingIndentationFixesIndentation() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()[|
        $$If True And
            False Then
        End If|]
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.Backspace()
                testData.EditorOperations.Backspace()
                testData.EditorOperations.Backspace()

                ' So far we should have had no commit
                testData.AssertHadCommit(False)

                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)

                Assert.Equal(testData.Workspace.Documents.Single().InitialTextSnapshot.GetText(), testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitIfThenOnlyAfterStartingNewBlock() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()[|
        $$|]
        If True Then
        End If
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("If True Then")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitEndIfOnlyAfterStartingNewBlock() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()
        If True Then
        End If[|
        $$|]
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("End If")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitFullIfBlockAfterCommittingElseIf() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()[|
        If True Then
        Dim x = 42
        $$|]
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("ElseIf False Then")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitFullIfBlockAfterCommittingEndIf() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()[|
        If True Then
        Dim x = 42
        $$|]
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("End If")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitTryBlockAfterCommittingCatch() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()[|
        Try
        Dim x = 42
        $$|]
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("Catch")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitTryBlockAfterCommittingFinally() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()[|
        Try
        Dim x = 42
        $$|]
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("Finally")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitDoLoopBlockAfterCommittingLoop() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()[|
        Do
        Dim x = 42
        $$|]
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("Loop")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitEnumBlockAfterCommittingEndEnum() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Namespace Program[|
    Enum Foo
        Alpha
        Bravo
        Charlie
    $$|]
End Namespace
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("End Enum")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitGetAccessorBlockAfterCommittingEndGet() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class Foo
    Public Property Bar As Integer[|
        Get
        $$|]
    End Property
End Namespace
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("End Get")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitSyncLockBlockAfterCommittingEndSyncLock() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class Foo
    Sub Foo()[|
        SyncLock Me
        Dim x = 42
        $$|]
    End Sub
End Class
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertText("End SyncLock")

                testData.AssertHadCommit(False)
                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(539613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539613")>
        Public Async Function TestRelativeIndentationBug() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Module1
    Dim a = Sub()
                Const x = 2
                If True Then
                    Console.WriteLine(x$$)
                End If
                Dim b = Sub()
                            Console.WriteLine()
                        End Sub
            End Function
    Sub Main()
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.Backspace()

                ' So far we should have had no commit
                testData.AssertHadCommit(False)

                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)

                Dim expected = <Code>
Module Module1
    Dim a = Sub()
                Const x = 2
                If True Then
                    Console.WriteLine()
                End If
                Dim b = Sub()
                            Console.WriteLine()
                        End Sub
            End Function
    Sub Main()
    End Sub
End Module
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WorkItem(16493, "DevDiv_Projects/Roslyn")>
        <WorkItem(539544, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539544")>
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestBetterStartIndentation() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        If 1 = 1 Then
        Else : If 1 = 1 Then
            $$Else : If 1 = 1 Then

            End If
            End If
        End If
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.Backspace()
                testData.EditorOperations.Backspace()

                ' So far we should have had no commit
                testData.AssertHadCommit(False)

                testData.EditorOperations.MoveLineUp(extendSelection:=False)
                testData.AssertHadCommit(True)

                Dim expected = <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        If 1 = 1 Then
        Else : If 1 = 1 Then
            Else : If 1 = 1 Then

            End If
            End If
        End If
    End Sub
End Module
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(544104, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544104")>
        Public Async Function TestCommitAfterMoveDownAfterIfStatement() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Sub Main()
        If True$$
    End Sub
End Class</Document>
                    </Project>
                </Workspace>)

                Dim expected = <Code>
Class C
    Sub Main()
        If True Then
    End Sub
End Class</Code>

                testData.EditorOperations.InsertText(" ")
                testData.EditorOperations.MoveLineDown(extendSelection:=False)

                ' The text should snap back to what it originally was
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterXmlElementStartTag() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>Class C
    Dim x = &lt;code
                &gt;$$
            &lt;/code&gt;
End Class</Document>
                    </Project>
                </Workspace>)

                Dim expected = <Code>Class C
    Dim x = &lt;code
                &gt;

            &lt;/code&gt;
End Class</Code>

                testData.EditorOperations.InsertNewLine()

                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <WorkItem(545358, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545358")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitWithNextStatementWithMultipleControlVariables() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>Module Program
    Sub S()
        For a = 1 To 3
            For b = 1 To 3
                For c = 1 To 3
                Next$$
        Next b, a
    End Sub
End Module</Document>
                    </Project>
                </Workspace>)

                Dim expected = <Code>Module Program
    Sub S()
        For a = 1 To 3
            For b = 1 To 3
                For c = 1 To 3
                Next

        Next b, a
    End Sub
End Module</Code>

                testData.EditorOperations.InsertNewLine()

                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <WorkItem(608438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608438")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestBugfix_608438() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>[|$$Imports System
Imports System.Linq

Module Program
    Sub Main(args As String())
        
    End Sub
End Module|]</Document>
                    </Project>
                </Workspace>)

                Dim document = testData.Workspace.Documents.Single()
                Dim onlyTextSpan = document.SelectedSpans.First()
                Dim snapshotspan = New SnapshotSpan(testData.Buffer.CurrentSnapshot, New Span(onlyTextSpan.Start, onlyTextSpan.Length))
                Dim view = document.GetTextView()
                view.Selection.Select(snapshotspan, isReversed:=False)
                Dim selArgs = New FormatSelectionCommandArgs(view, document.GetTextBuffer())
                testData.CommandHandler.ExecuteCommand(selArgs, Sub() Return)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(924578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924578")>
        Public Async Function TestMultiLineString1() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Sub M()
        Dim s = "$$"
    End Sub
End Class
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()

                Dim expected = <Code>
Class C
    Sub M()
        Dim s = "
"
    End Sub
End Class
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(924578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924578")>
        Public Async Function TestMultiLineString2() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Sub M(s As String)
        M($$"")
    End Sub
End Class
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()

                Dim expected = <Code>
Class C
    Sub M(s As String)
        M(
            "")
    End Sub
End Class
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(924578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/924578")>
        Public Async Function TestMultiLineString3() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Sub M(s As String)
        M(""$$)
    End Sub
End Class
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()

                Dim expected = <Code>
Class C
    Sub M(s As String)
        M(""
          )
    End Sub
End Class
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestEnableWarningDirective1() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
        #   enable     warning    bc123,[bc456],BC789      '  Comment$$

</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()
                testData.EditorOperations.MoveLineDown(False)

                Dim expected = <Code>
#Enable Warning bc123, [bc456], BC789      '  Comment


</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestEnableWarningDirective2() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
        #   enable     warning    $$

</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()
                testData.EditorOperations.MoveLineDown(False)

                Dim expected = <Code>
#Enable Warning


</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestDisableWarningDirective1() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module Program
    Sub Main()
        #disable       warning         bc123,            [bc456] _$$, someOtherId
    End Sub
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()
                testData.EditorOperations.MoveLineDown(False)

                Dim expected = <Code>
Module Program
    Sub Main()
#Disable Warning bc123, [bc456] _
        , someOtherId
    End Sub
End Module
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestDisableWarningDirective2() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M1
        #   disable     warning    $$ 'Comment
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()
                testData.EditorOperations.MoveLineDown(False)

                Dim expected = <Code>
Module M1
#Disable Warning
    'Comment
End Module
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestIncompleteWarningDirective() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Module M1
        #   enable     warning[BC123] ,  $$
End Module
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()
                testData.EditorOperations.MoveLineDown(False)

                Dim expected = <Code>
Module M1
#Enable Warning [BC123],

End Module
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WorkItem(3119, "https://github.com/dotnet/roslyn/issues/3119")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestMissingThenInIf() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Sub M()
        If True $$
            M()
        End If
    End Sub
End Class
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()
                testData.EditorOperations.MoveLineDown(False)

                Dim expected = <Code>
Class C
    Sub M()
        If True Then

            M()
        End If
    End Sub
End Class
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WorkItem(3119, "https://github.com/dotnet/roslyn/issues/3119")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestMissingThenInElseIf() As Task
            Using testData = Await CommitTestData.CreateAsync(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Class C
    Sub M()
        If True Then
            M()
        ElseIf False $$
            M()
        End If
    End Sub
End Class
</Document>
                    </Project>
                </Workspace>)

                testData.EditorOperations.InsertNewLine()
                testData.EditorOperations.MoveLineDown(False)

                Dim expected = <Code>
Class C
    Sub M()
        If True Then
            M()
        ElseIf False Then

            M()
        End If
    End Sub
End Class
</Code>
                Assert.Equal(expected.NormalizedValue, testData.Workspace.Documents.Single().TextBuffer.CurrentSnapshot.GetText())
            End Using
        End Function
    End Class
End Namespace
