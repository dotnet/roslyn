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

        Private Const UnformattedPastedCode As String = "
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
        Public Async Function PasteTracking_MissingTextSpan_WhenNothingPasted() As Task
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                Await testState.AssertMissingPastedTextSpanAsync(class1Document)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasTextSpan_AfterPaste() As Task
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                Dim expectedTextSpan = testState.SendPaste(class1Document, PastedCode)

                Await testState.AssertHasPastedTextSpanAsync(class1Document, expectedTextSpan)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasTextSpan_AfterFormattingPaste() As Task
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                Dim expectedTextSpan = testState.SendPaste(class1Document, UnformattedPastedCode)

                Await testState.AssertHasPastedTextSpanAsync(class1Document, expectedTextSpan)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasTextSpan_AfterMultiplePastes() As Task
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                Dim firstTextSpan = testState.SendPaste(class1Document, PastedCode)
                Dim expectedTextSpan = testState.SendPaste(class1Document, PastedCode)

                Assert.NotEqual(firstTextSpan, expectedTextSpan)

                Await testState.AssertHasPastedTextSpanAsync(class1Document, expectedTextSpan)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_MissingTextSpan_AfterPasteThenEdit() As Task
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.InsertText(class1Document, "Foo")

                Await testState.AssertMissingPastedTextSpanAsync(class1Document)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_MissingTextSpan_AfterPasteThenClose() As Task
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.CloseDocument(class1Document)

                Await testState.AssertMissingPastedTextSpanAsync(class1Document)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_MissingTextSpan_AfterPasteThenCloseThenOpen() As Task
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)
                testState.CloseDocument(class1Document)

                testState.OpenDocument(class1Document)

                Await testState.AssertMissingPastedTextSpanAsync(class1Document)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasTextSpan_AfterPasteThenCloseThenOpenThenPaste() As Task
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)
                testState.CloseDocument(class1Document)
                testState.OpenDocument(class1Document)

                Dim expectedTextSpan = testState.SendPaste(class1Document, PastedCode)

                Await testState.AssertHasPastedTextSpanAsync(class1Document, expectedTextSpan)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasMultipleTextSpan_AfterPasteInMultipleFiles() As Task
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class2Document = testState.OpenDocument(Project1Name, Class2Name)

                Dim expectedClass1TextSpan = testState.SendPaste(class1Document, PastedCode)
                Dim expectedClass2TextSpan = testState.SendPaste(class2Document, PastedCode)

                Assert.NotEqual(expectedClass1TextSpan, expectedClass2TextSpan)

                Await testState.AssertHasPastedTextSpanAsync(class1Document, expectedClass1TextSpan)
                Await testState.AssertHasPastedTextSpanAsync(class2Document, expectedClass2TextSpan)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasSingleTextSpan_AfterPasteInMultipleFilesThenOneClosed() As Task
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class2Document = testState.OpenDocument(Project1Name, Class2Name)

                testState.SendPaste(class1Document, PastedCode)
                Dim expectedClass2TextSpan = testState.SendPaste(class2Document, PastedCode)

                testState.CloseDocument(class1Document)

                Await testState.AssertMissingPastedTextSpanAsync(class1Document)
                Await testState.AssertHasPastedTextSpanAsync(class2Document, expectedClass2TextSpan)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_MissingTextSpan_AfterPasteInMultipleFilesThenAllClosed() As Task
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class2Document = testState.OpenDocument(Project1Name, Class2Name)

                testState.SendPaste(class1Document, PastedCode)
                testState.SendPaste(class2Document, PastedCode)

                testState.CloseDocument(class1Document)
                testState.CloseDocument(class2Document)

                Await testState.AssertMissingPastedTextSpanAsync(class1Document)
                Await testState.AssertMissingPastedTextSpanAsync(class2Document)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasTextSpanInLinkedFile_AfterPaste() As Task
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                Dim expectedClass1TextSpan = testState.SendPaste(class1Document, PastedCode)

                Await testState.AssertHasPastedTextSpanAsync(class1LinkedDocument, expectedClass1TextSpan)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasTextSpanInLinkedFile_AfterPasteThenClose() As Task
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                Dim expectedClass1TextSpan = testState.SendPaste(class1Document, PastedCode)

                testState.CloseDocument(class1Document)

                Await testState.AssertHasPastedTextSpanAsync(class1LinkedDocument, expectedClass1TextSpan)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_MissingTextSpanForLinkedFile_AfterPasteThenCloseAll() As Task
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.CloseDocument(class1Document)
                testState.CloseDocument(class1LinkedDocument)

                Await testState.AssertMissingPastedTextSpanAsync(class1LinkedDocument)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_MissingTextSpan_AfterPasteThenLinkedFileEdited() As Task
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)
                testState.InsertText(class1LinkedDocument, "Foo")

                Await testState.AssertMissingPastedTextSpanAsync(class1Document)
                Await testState.AssertMissingPastedTextSpanAsync(class1LinkedDocument)
            End Using
        End Function


        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasTextSpanForLinkedFile_AfterPasteThenCloseAllThenOpenThenPaste() As Task
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.CloseDocument(class1Document)
                testState.CloseDocument(class1LinkedDocument)

                Await testState.AssertMissingPastedTextSpanAsync(class1LinkedDocument)

                testState.OpenDocument(class1LinkedDocument)

                Dim expectedTextSpan = testState.SendPaste(class1LinkedDocument, PastedCode)

                Await testState.AssertHasPastedTextSpanAsync(class1LinkedDocument, expectedTextSpan)
            End Using
        End Function

    End Class
End Namespace
