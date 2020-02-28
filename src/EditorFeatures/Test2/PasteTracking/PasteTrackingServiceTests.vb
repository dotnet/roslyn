﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        Public Sub PasteTracking_MissingTextSpan_WhenNothingPasted()
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.AssertMissingPastedTextSpan(class1Document.GetTextBuffer())
            End Using
        End Sub

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
        Public Sub PasteTracking_MissingTextSpan_AfterPasteThenEdit()
            Using testState = New PasteTrackingTestState(SingleFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.InsertText(class1Document, "Foo")

                testState.AssertMissingPastedTextSpan(class1Document.GetTextBuffer())
            End Using
        End Sub

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

                Await testState.AssertHasPastedTextSpanAsync(class2Document, expectedClass2TextSpan)
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
        Public Sub PasteTracking_MissingTextSpan_AfterPasteThenLinkedFileEdited()
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)
                testState.InsertText(class1LinkedDocument, "Foo")

                testState.AssertMissingPastedTextSpan(class1Document.GetTextBuffer())
                testState.AssertMissingPastedTextSpan(class1LinkedDocument.GetTextBuffer())
            End Using
        End Sub


        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.PasteTracking)>
        Public Async Function PasteTracking_HasTextSpanForLinkedFile_AfterPasteThenCloseAllThenOpenThenPaste() As Task
            Using testState = New PasteTrackingTestState(MultiFileCode)
                Dim class1Document = testState.OpenDocument(Project1Name, Class1Name)
                Dim class1LinkedDocument = testState.OpenDocument(Project2Name, Class1Name)

                testState.SendPaste(class1Document, PastedCode)

                testState.CloseDocument(class1Document)
                testState.CloseDocument(class1LinkedDocument)

                testState.OpenDocument(class1LinkedDocument)

                Dim expectedTextSpan = testState.SendPaste(class1LinkedDocument, PastedCode)

                Await testState.AssertHasPastedTextSpanAsync(class1LinkedDocument, expectedTextSpan)
            End Using
        End Function

    End Class
End Namespace
