' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.LineCommit
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.LineCommit)>
    Public Class CommitOnEnterTests
        <WpfFact>
        Public Sub TestCommitAfterEnterOnSimpleStatement()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
imports System$$|]
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact>
        Public Sub TestNoCommitAfterEnterAfterQuery()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()
        Dim x = From x In { 1, 2, 3 }$$
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=False)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531421")>
        Public Sub TestNoCommitAfterExplicitLineContinuation()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()
        M() _$$
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=False)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531421")>
        Public Sub TestNoCommitAfterExplicitLineContinuationCommentsAfterLineContinuation()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()
        M() _ ' Test$$
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=False)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531421")>
        Public Sub TestCommitAfterBlankLineFollowingExplicitLineContinuation()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()[|
        M() _
            |]$$
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact>
        Public Sub TestCommitAfterDeclaration()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
Class C$$
    Sub M()
        m() _
            
    End Sub
End Class|]
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact>
        Public Sub TestCommitAfterEndConstruct()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
Class C
    Sub M()
        m() _
            
    End Sub
End Class$$|]
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True, usedSemantics:=False)
        End Sub

        <WpfFact>
        Public Sub TestCommitAfterBlankLineAfterQuery()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()[|
        Dim x = From x In { 1, 2, 3 }
                $$|]
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact>
        Public Sub TestNoCommitAfterEnterAfterPartialExpression()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()
        Dim x = 1 + $$
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=False)
        End Sub

        <WpfFact>
        Public Sub TestCommitAfterEnterAfterPartialExpression()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()
        Dim x = 1 + [|
            $$|]
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact>
        Public Sub TestCommitAfterEnterOnBlankLine()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()[|
            $$|]
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539451")>
        Public Sub TestCommitAfterColon()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()[|
        Call     M() : $$|]
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539408")>
        Public Sub TestCommitAfterConstDirective()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
#const goo = 42$$|]
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539408")>
        Public Sub TestCommitAfterComment()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
rem Hello World$$|]
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544372")>
        Public Sub UndoAfterCommitOnBlankLine()
            Using testData = CommitTestData.Create(<Workspace>
                                                       <Project Language="Visual Basic" CommonReferences="true">
                                                           <Document>$$
                                                        </Document>
                                                       </Project>
                                                   </Workspace>)

                testData.CommandHandler.ExecuteCommand(New ReturnKeyCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertNewLine(), TestCommandExecutionContext.Create())
                testData.UndoHistory.Undo(count:=1)

                Assert.Equal(0, testData.View.Caret.Position.BufferPosition.GetContainingLineNumber())
            End Using
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540210")>
        Public Sub TestCommitAfterThenTouchingThen()
            ' Note that the source we are starting this test with is *not* syntactically correct,
            ' but by having the extra "End If" we guarantee the ending code will be as if End
            ' Construct generation happened.
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()[|
        If True Then$$ q = Sub()
                           End Sub|]
        End If
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540210")>
        Public Sub TestCommitAfterThenTouchingStatement()
            ' Note that the source we are starting this test with is *not* syntactically correct,
            ' but by having the extra "End If" we guarantee the ending code will be as if End
            ' Construct generation happened.
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class C
    Sub M()[|
        If True Then $$q = Sub()
                           End Sub|]
        End If
    End Sub
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530463")>
        Public Sub TestCommitAfterPropertyStatement()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Class A
    Property P1[|
    Property P2$$|]
    Property P3
End Class
 
Class E
    Property P1
End Class
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/986168")>
        Public Sub TestDoNotCommitInsideStringLiteral()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>
Module M
    Sub M()
        Dim s = "$$
        Console.WriteLine("The method or operation is not implemented.")
    End Sub
End Module
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=False)
        End Sub

        Private Shared Sub AssertCommitsStatement(test As XElement, expectCommit As Boolean, Optional usedSemantics As Boolean = True)
            Using testData = CommitTestData.Create(test)
                Dim lineNumber = testData.View.Caret.Position.BufferPosition.GetContainingLineNumber()
                testData.CommandHandler.ExecuteCommand(New ReturnKeyCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertNewLine(), TestCommandExecutionContext.Create())
                testData.AssertHadCommit(expectCommit)
                If expectCommit Then
                    testData.AssertUsedSemantics(usedSemantics)
                End If

                Assert.Equal(lineNumber + 1, testData.View.Caret.Position.BufferPosition.GetContainingLineNumber())
            End Using
        End Sub
    End Class
End Namespace
