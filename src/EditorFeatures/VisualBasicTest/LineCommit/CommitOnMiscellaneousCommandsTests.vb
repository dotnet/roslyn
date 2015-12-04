' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.LineCommit
    Public Class CommitOnMiscellaneousCommandsTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitOnMultiLinePaste() As Task
            Using testData = Await CommitTestData.CreateAsync(<Workspace>
                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                      <Document>$$
                                                        </Document>
                                                                  </Project>
                                                              </Workspace>)

                testData.CommandHandler.ExecuteCommand(New PasteCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertText("  imports  system" & vbCrLf & "  imports system.text"))
                Assert.Equal("Imports System", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(0).GetText())
            End Using
        End Function

        <WpfFact>
        <WorkItem(1944, "https://github.com/dotnet/roslyn/issues/1944")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestDontCommitOnMultiLinePasteWithPrettyListingOff() As Task
            Using testData = Await CommitTestData.CreateAsync(<Workspace>
                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                      <Document>$$
                                                        </Document>
                                                                  </Project>
                                                              </Workspace>)
                testData.Workspace.Options = testData.Workspace.Options.WithChangedOption(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic, False)
                testData.CommandHandler.ExecuteCommand(New PasteCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertText("Class Program" & vbCrLf & "    Sub M(abc As Integer)" & vbCrLf & "        Dim a  = 7" & vbCrLf & "    End Sub" & vbCrLf & "End Class"))
                Assert.Equal("        Dim a  = 7", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(2).GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        <WorkItem(545493)>
        Public Async Function TestNoCommitOnSingleLinePaste() As Task
            Using testData = Await CommitTestData.CreateAsync(<Workspace>
                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                      <Document>$$
                                                        </Document>
                                                                  </Project>
                                                              </Workspace>)

                testData.CommandHandler.ExecuteCommand(New PasteCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertText("  imports  system"))
                Assert.Equal("  imports  system", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(0).GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestCommitOnSave() As Task
            Using testData = Await CommitTestData.CreateAsync(<Workspace>
                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                      <Document>$$
                                                        </Document>
                                                                  </Project>
                                                              </Workspace>)

                testData.Buffer.Insert(0, "  imports  system")
                testData.CommandHandler.ExecuteCommand(New SaveCommandArgs(testData.View, testData.Buffer), Sub() Exit Sub)
                Assert.Equal("Imports System", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(0).GetText())
            End Using
        End Function

        <WpfFact, WorkItem(1944, "https://github.com/dotnet/roslyn/issues/1944")>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestDontCommitOnSavePrettyListingOff() As Task
            Using testData = Await CommitTestData.CreateAsync(<Workspace>
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
                testData.CommandHandler.ExecuteCommand(New SaveCommandArgs(testData.View, testData.Buffer), Sub() Exit Sub)
                Assert.Equal("        Dim a     = 7", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(3).GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(545493)>
        Public Async Function TestPerformAddMissingTokenOnFormatDocument() As Task
            Using testData = Await CommitTestData.CreateAsync(<Workspace>
                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                      <Document>$$Module Program
    Sub Main()
        foo
    End Sub
 
    Private Sub foo()
    End Sub
End Module
                                                        </Document>
                                                                  </Project>
                                                              </Workspace>)

                testData.CommandHandler.ExecuteCommand(New FormatDocumentCommandArgs(testData.View, testData.Buffer), Sub() Exit Sub)
                Assert.Equal("        foo()", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(2).GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Formatting)>
        <WorkItem(867153)>
        Public Async Function TestFormatDocumentWithPrettyListingDisabled() As Task
            Using testData = Await CommitTestData.CreateAsync(<Workspace>
                                                                  <Project Language="Visual Basic" CommonReferences="true">
                                                                      <Document>Module Program
        $$Sub Main()
        foo
    End Sub
End Module
                                                        </Document>
                                                                  </Project>
                                                              </Workspace>)

                ' Turn off pretty listing
                Dim optionService = testData.Workspace.GetService(Of IOptionService)()
                Dim optionSet = optionService.GetOptions()
                Dim prettyListing = optionSet.WithChangedOption(FeatureOnOffOptions.PrettyListing, LanguageNames.VisualBasic, False)
                optionService.SetOptions(prettyListing)

                testData.CommandHandler.ExecuteCommand(New FormatDocumentCommandArgs(testData.View, testData.Buffer), Sub() Exit Sub)
                Assert.Equal("    Sub Main()", testData.Buffer.CurrentSnapshot.GetLineFromLineNumber(1).GetText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.LineCommit)>
        Public Async Function TestDoNotCommitWithUnterminatedString() As Task
            Using testData = Await CommitTestData.CreateAsync(<Workspace>
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

                testData.CommandHandler.ExecuteCommand(New PasteCommandArgs(testData.View, testData.Buffer), Sub() testData.EditorOperations.InsertText("Console.WriteLine(""Hello World"))
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
        End Function
    End Class
End Namespace
