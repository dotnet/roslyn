' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Public Sub CommitAfterEnterOnSimpleStatement()
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
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub NoCommitAfterEnterAfterQuery()
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

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(531421)>
        Public Sub NoCommitAfterExplicitLineContinuation()
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

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(531421)>
        Public Sub CommitAfterBlankLineFollowingExplicitLineContinuation()
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
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterDeclaration()
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
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterEndConstruct()
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
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterBlankLineAfterQuery()
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
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub NoCommitAfterEnterAfterPartialExpression()
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
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterEnterAfterPartialExpression()
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
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterEnterOnBlankLine()
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

        <WpfFact>
        <WorkItem(539451)>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterColon()
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

        <WpfFact>
        <WorkItem(539408)>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterConstDirective()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
#const foo = 42$$|]
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact>
        <WorkItem(539408)>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterComment()
            Dim test = <Workspace>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document>[|
rem Hello World$$|]
                               </Document>
                           </Project>
                       </Workspace>

            AssertCommitsStatement(test, expectCommit:=True)
        End Sub

        <WpfFact>
        <WorkItem(544372)>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub UndoAfterCommitOnBlankLine()
            Using testData = New CommitTestData(<Workspace>
                                                    <Project Language="Visual Basic" CommonReferences="true">
                                                        <Document>$$
                                                        </Document>
                                                    </Project>
                                                </Workspace>)

                testData.CommandHandler.ExecuteCommand(New ReturnKeyCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertNewLine())
                testData.UndoHistory.Undo(count:=1)

                Assert.Equal(0, testData.View.Caret.Position.BufferPosition.GetContainingLine().LineNumber)
            End Using
        End Sub

        <WpfFact>
        <WorkItem(540210)>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterThenTouchingThen()
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

        <WpfFact>
        <WorkItem(540210)>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterThenTouchingStatement()
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

        <WpfFact>
        <WorkItem(530463)>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub CommitAfterPropertyStatement()
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

        <WpfFact>
        <WorkItem(986168)>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub DontCommitInsideStringLiteral()
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

        Private Sub AssertCommitsStatement(test As XElement, expectCommit As Boolean, Optional usedSemantics As Boolean = True)
            Using testData = New CommitTestData(test)
                Dim lineNumber = testData.View.Caret.Position.BufferPosition.GetContainingLine().LineNumber
                testData.CommandHandler.ExecuteCommand(New ReturnKeyCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertNewLine())
                testData.AssertHadCommit(expectCommit)
                If expectCommit Then
                    testData.AssertUsedSemantics(usedSemantics)
                End If

                Assert.Equal(lineNumber + 1, testData.View.Caret.Position.BufferPosition.GetContainingLine().LineNumber)
            End Using
        End Sub
    End Class
End Namespace
