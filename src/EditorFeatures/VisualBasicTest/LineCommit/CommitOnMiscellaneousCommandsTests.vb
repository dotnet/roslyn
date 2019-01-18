' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.LineCommit
    <[UseExportProvider]>
    Public Class CommitOnMiscellaneousCommandsTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub TestCommitOnMultiLinePaste()
            Using testData = CommitTestData.Create(<Workspace>
                                                       <Project Language="Visual Basic" CommonReferences="true">
                                                           <Document>$$
                                                        </Document>
                                                       </Project>
                                                   </Workspace>)

                testData.CommandHandler.ExecuteCommand(New PasteCommandArgs(testData.View, testData.Buffer),
                                                       Sub() testData.EditorOperations.InsertText("  imports  system" & vbCrLf & "  imports system.text"),
                                                       TestCommandExecutionContext.Create())
                Assert.Equal("Imports System", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(0).GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(14391, "https://github.com/dotnet/roslyn/issues/14391")>
        Public Sub TestCommitLineWithTupleType()
            Using testData = CommitTestData.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>
Imports System
Public Class C
    Shared Sub Main()
        Dim t As (In$$)
    End Sub
    Shared Sub Int()
    End Sub
End Class
                        </Document>
                    </Project>
                </Workspace>)

                testData.CommandHandler.ExecuteCommand(New PasteCommandArgs(testData.View, testData.Buffer),
                                                       Sub() testData.EditorOperations.InsertText("t"),
                                                       TestCommandExecutionContext.Create())
                testData.CommandHandler.ExecuteCommand(New SaveCommandArgs(testData.View, testData.Buffer),
                                                       Sub() Exit Sub,
                                                       TestCommandExecutionContext.Create())
                testData.AssertHadCommit(True)
                ' The code cleanup should not add parens after the Int, so no exception.
            End Using
        End Sub

        <WpfFact>
        <WorkItem(1944, "https://github.com/dotnet/roslyn/issues/1944")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub TestDontCommitOnMultiLinePasteWithPrettyListingOff()
            Using testData = CommitTestData.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <Document>$$
                        </Document>
                    </Project>
                </Workspace>)

                testData.Workspace.Options = testData.Workspace.Options.WithChangedOption(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic, False)
                testData.CommandHandler.ExecuteCommand(New PasteCommandArgs(testData.View, testData.Buffer),
                                                       Sub() testData.EditorOperations.InsertText("Class Program" & vbCrLf & "    Sub M(abc As Integer)" & vbCrLf & "        Dim a  = 7" & vbCrLf & "    End Sub" & vbCrLf & "End Class"),
                                                       TestCommandExecutionContext.Create())
                Assert.Equal("        Dim a  = 7", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(2).GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(545493, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545493")>
        Public Sub TestNoCommitOnSingleLinePaste()
            Using testData = CommitTestData.Create(<Workspace>
                                                       <Project Language="Visual Basic" CommonReferences="true">
                                                           <Document>$$
                                                        </Document>
                                                       </Project>
                                                   </Workspace>)

                testData.CommandHandler.ExecuteCommand(New PasteCommandArgs(testData.View, testData.Buffer),
                                                       Sub() testData.EditorOperations.InsertText("  imports  system"),
                                                       TestCommandExecutionContext.Create())
                Assert.Equal("  imports  system", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(0).GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub TestCommitOnSave()
            Using testData = CommitTestData.Create(<Workspace>
                                                       <Project Language="Visual Basic" CommonReferences="true">
                                                           <Document>$$
                                                        </Document>
                                                       </Project>
                                                   </Workspace>)

                testData.Buffer.Insert(0, "  imports  system")
                testData.CommandHandler.ExecuteCommand(New SaveCommandArgs(testData.View, testData.Buffer), Sub() Exit Sub, TestCommandExecutionContext.Create())
                Assert.Equal("Imports System", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(0).GetText())
            End Using
        End Sub

        <WpfFact, WorkItem(1944, "https://github.com/dotnet/roslyn/issues/1944")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub TestDontCommitOnSavePrettyListingOff()
            Using testData = CommitTestData.Create(<Workspace>
                                                       <Project Language="Visual Basic" CommonReferences="true">
                                                           <Document>
Class Program
    Sub M(abc As Integer)
        Dim a $$= 7
    End Sub
End Class
                                                        </Document>
                                                       </Project>
                                                   </Workspace>)
                testData.Workspace.Options = testData.Workspace.Options.WithChangedOption(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic, False)
                testData.Buffer.Insert(57, "    ")
                testData.CommandHandler.ExecuteCommand(New SaveCommandArgs(testData.View, testData.Buffer), Sub() Exit Sub, TestCommandExecutionContext.Create())
                Assert.Equal("        Dim a     = 7", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(3).GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(545493, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545493")>
        Public Sub TestPerformAddMissingTokenOnFormatDocument()
            Using testData = CommitTestData.Create(<Workspace>
                                                       <Project Language="Visual Basic" CommonReferences="true">
                                                           <Document>$$Module Program
    Sub Main()
        goo
    End Sub
 
    Private Sub goo()
    End Sub
End Module
                                                        </Document>
                                                       </Project>
                                                   </Workspace>)

                testData.CommandHandler.ExecuteCommand(New FormatDocumentCommandArgs(testData.View, testData.Buffer), Sub() Exit Sub, TestCommandExecutionContext.Create())
                Assert.Equal("        goo()", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(2).GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(867153, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867153")>
        Public Sub TestFormatDocumentWithPrettyListingDisabled()
            Using testData = CommitTestData.Create(<Workspace>
                                                       <Project Language="Visual Basic" CommonReferences="true">
                                                           <Document>Module Program
        $$Sub Main()
        goo
    End Sub
End Module
                                                        </Document>
                                                       </Project>
                                                   </Workspace>)

                ' Turn off pretty listing
                testData.Workspace.Options = testData.Workspace.Options.WithChangedOption(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic, False)
                testData.CommandHandler.ExecuteCommand(New FormatDocumentCommandArgs(testData.View, testData.Buffer), Sub() Exit Sub, TestCommandExecutionContext.Create())
                Assert.Equal("    Sub Main()", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(1).GetText())
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Sub TestDoNotCommitWithUnterminatedString()
            Using testData = CommitTestData.Create(<Workspace>
                                                       <Project Language="Visual Basic" CommonReferences="true">
                                                           <Document>Module Module1
    Sub Main()
        $$
    End Sub

    Sub SomeUnrelatedCode()
        Console.WriteLine("&lt;a&gt;")
    End Sub
End Module</Document>
                                                       </Project>
                                                   </Workspace>)

                testData.CommandHandler.ExecuteCommand(New PasteCommandArgs(testData.View, testData.Buffer),
                                                       Sub() testData.EditorOperations.InsertText("Console.WriteLine(""Hello World"),
                                                       TestCommandExecutionContext.Create())
                Assert.Equal(testData.Buffer.CurrentSnapshot.GetText(),
<Document>Module Module1
    Sub Main()
        Console.WriteLine("Hello World
    End Sub

    Sub SomeUnrelatedCode()
        Console.WriteLine("&lt;a&gt;")
    End Sub
End Module</Document>.NormalizedValue)
            End Using
        End Sub
    End Class
End Namespace
