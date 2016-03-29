' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.LineCommit
    Public Class CommitOnEnterTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterEnterOnSimpleStatement() As Task
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
imports System$$|]
                               </Document>
                           </Project>
                       </Workspace>

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestNoCommitAfterEnterAfterQuery() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=False)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(531421, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531421")>
        Public Async Function TestNoCommitAfterExplicitLineContinuation() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=False)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(531421, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531421")>
        Public Async Function TestCommitAfterBlankLineFollowingExplicitLineContinuation() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterDeclaration() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterEndConstruct() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True, usedSemantics:=False)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterBlankLineAfterQuery() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestNoCommitAfterEnterAfterPartialExpression() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=False)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterEnterAfterPartialExpression() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterEnterOnBlankLine() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <WorkItem(539451, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539451")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterColon() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <WorkItem(539408, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539408")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterConstDirective() As Task
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
#const foo = 42$$|]
                               </Document>
                           </Project>
                       </Workspace>

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <WorkItem(539408, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539408")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterComment() As Task
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
rem Hello World$$|]
                               </Document>
                           </Project>
                       </Workspace>

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <WorkItem(544372, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544372")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function UndoAfterCommitOnBlankLine() As Threading.Tasks.Task
            Using testData = Await CommitTestData.CreateAsync(<Workspace>
                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                      <Document>$$
                                                        </Document>
                                                                  </Project>
                                                              </Workspace>)

                testData.CommandHandler.ExecuteCommand(New ReturnKeyCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertNewLine())
                testData.UndoHistory.Undo(count:=1)

                Assert.Equal(0, testData.View.Caret.Position.BufferPosition.GetContainingLine().LineNumber)
            End Using
        End Function

        <WpfFact>
        <WorkItem(540210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540210")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterThenTouchingThen() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <WorkItem(540210, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540210")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterThenTouchingStatement() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <WorkItem(530463, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530463")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitAfterPropertyStatement() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=True)
        End Function

        <WpfFact>
        <WorkItem(986168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/986168")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestDontCommitInsideStringLiteral() As Task
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

            Await AssertCommitsStatementAsync(test, expectCommit:=False)
        End Function

        Private Async Function AssertCommitsStatementAsync(test As XElement, expectCommit As Boolean, Optional usedSemantics As Boolean = True) As Threading.Tasks.Task
            Using testData = Await CommitTestData.CreateAsync(test)
                Dim lineNumber = testData.View.Caret.Position.BufferPosition.GetContainingLine().LineNumber
                testData.CommandHandler.ExecuteCommand(New ReturnKeyCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertNewLine())
                testData.AssertHadCommit(expectCommit)
                If expectCommit Then
                    testData.AssertUsedSemantics(usedSemantics)
                End If

                Assert.Equal(lineNumber + 1, testData.View.Caret.Position.BufferPosition.GetContainingLine().LineNumber)
            End Using
        End Function
    End Class
End Namespace
