' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.PasteTracking

    <UseExportProvider>
    Public Class PasteTrackingServiceTests

        Private Const Project1Name = "Proj1"
        Private Const Project2Name = "Proj2"
        Private Const Class1Name = "Class1.cs"
        Private Const Class2Name = "Class2.cs"

        Private Const PastedCode As String = "
public void Main(string[] args)
{
}"

        Private ReadOnly Property SingleFileCode As XElement =
            <Workspace>
                <Project Language="C#" CommonReferences="True" AssemblyName="Proj1">
                    <Document FilePath="Class1.cs">
                        public class Class1
                        {
                        $$
                        }
                    </Document>
                </Project>
            </Workspace>

        Private ReadOnly Property MultiFileCode As XElement =
            <Workspace>
                <Project Language="C#" CommonReferences="True" AssemblyName=<%= Project1Name %>>
                    <Document FilePath=<%= Class1Name %>>
                        public class Class1
                        {
                        $$
                        }
                    </Document>
                    <Document FilePath=<%= Class2Name %>>
                        public class Class2
                        {
                            public const string Greeting = "Hello";

                        $$
                        }
                    </Document>
                </Project>
                <Project Language="C#" CommonReferences="True" AssemblyName=<%= Project2Name %>>
                    <Document IsLinkFile="True" LinkAssemblyName="Proj1" LinkFilePath=<%= Class1Name %>/>
                </Project>
            </Workspace>

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_MissingTextSpan_WhenNothingPasted()
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.AssertMissingPastedTextSpan(class1Document)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_HasTextSpan_AfterPaste()
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                Dim expectedTextSpan = testState.SendPaste(class1Document, PastedCode)

                testState.AssertHasPastedTextSpan(class1Document, expectedTextSpan)
            End Using
        End Sub


        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_HasTextSpan_AfterMultiplePastes()
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                Dim firstTextSpan = testState.SendPaste(class1Document, PastedCode)
                Dim expectedTextSpan = testState.SendPaste(class1Document, PastedCode)

                Assert.NotEqual(firstTextSpan, expectedTextSpan)

                testState.AssertHasPastedTextSpan(class1Document, expectedTextSpan)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_MissingTextSpan_AfterPasteThenEdit()
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.InsertText(class1Document, "Foo")

                testState.AssertMissingPastedTextSpan(class1Document)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_MissingTextSpan_AfterPasteThenClose()
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.CloseDocument(class1Document)

                testState.AssertMissingPastedTextSpan(class1Document)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_MissingTextSpan_AfterPasteThenCloseThenOpen()
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)
                testState.CloseDocument(class1Document)

                testState.OpenDocument(class1Document)

                testState.AssertMissingPastedTextSpan(class1Document)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_HasTextSpan_AfterPasteThenCloseThenOpenThenPaste()
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)
                testState.CloseDocument(class1Document)
                testState.OpenDocument(class1Document)

                Dim expectedTextSpan = testState.SendPaste(class1Document, PastedCode)

                testState.AssertHasPastedTextSpan(class1Document, expectedTextSpan)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_HasMultipleTextSpan_AfterPasteInMultipleFiles()
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class2Document = testState.OpenDocument(Project1Name, Class2Name)

                Dim expectedClass1TextSpan = testState.SendPaste(class1Document, PastedCode)
                Dim expectedClass2TextSpan = testState.SendPaste(class2Document, PastedCode)

                Assert.NotEqual(expectedClass1TextSpan, expectedClass2TextSpan)

                testState.AssertHasPastedTextSpan(class1Document, expectedClass1TextSpan)
                testState.AssertHasPastedTextSpan(class2Document, expectedClass2TextSpan)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_HasSingleTextSpan_AfterPasteInMultipleFilesThenOneClosed()
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class2Document = testState.OpenDocument(Project1Name, Class2Name)

                testState.SendPaste(class1Document, PastedCode)
                Dim expectedClass2TextSpan = testState.SendPaste(class2Document, PastedCode)

                testState.CloseDocument(class1Document)

                testState.AssertMissingPastedTextSpan(class1Document)
                testState.AssertHasPastedTextSpan(class2Document, expectedClass2TextSpan)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_MissingTextSpan_AfterPasteInMultipleFilesThenAllClosed()
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class2Document = testState.OpenDocument(Project1Name, Class2Name)

                testState.SendPaste(class1Document, PastedCode)
                testState.SendPaste(class2Document, PastedCode)

                testState.CloseDocument(class1Document)
                testState.CloseDocument(class2Document)

                testState.AssertMissingPastedTextSpan(class1Document)
                testState.AssertMissingPastedTextSpan(class2Document)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_HasTextSpanInLinkedFile_AfterPaste()
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                Dim expectedClass1TextSpan = testState.SendPaste(class1Document, PastedCode)

                testState.AssertHasPastedTextSpan(class1LinkedDocument, expectedClass1TextSpan)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_HasTextSpanInLinkedFile_AfterPasteThenClose()
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                Dim expectedClass1TextSpan = testState.SendPaste(class1Document, PastedCode)

                testState.CloseDocument(class1Document)

                testState.AssertHasPastedTextSpan(class1LinkedDocument, expectedClass1TextSpan)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_MissingTextSpanForLinkedFile_AfterPasteThenCloseAll()
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.CloseDocument(class1Document)
                testState.CloseDocument(class1LinkedDocument)

                testState.AssertMissingPastedTextSpan(class1LinkedDocument)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_MissingTextSpan_AfterPasteThenLinkedFileEdited()
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)
                testState.InsertText(class1LinkedDocument, "Foo")

                testState.AssertMissingPastedTextSpan(class1Document)
                testState.AssertMissingPastedTextSpan(class1LinkedDocument)
            End Using
        End Sub


        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Sub PasteTracking_HasTextSpanForLinkedFile_AfterPasteThenCloseAllThenOpenThenPaste()
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.CloseDocument(class1Document)
                testState.CloseDocument(class1LinkedDocument)

                testState.AssertMissingPastedTextSpan(class1LinkedDocument)

                testState.OpenDocument(class1LinkedDocument)

                Dim expectedTextSpan = testState.SendPaste(class1LinkedDocument, PastedCode)

                testState.AssertHasPastedTextSpan(class1LinkedDocument, expectedTextSpan)
            End Using
        End Sub

    End Class
End Namespace
