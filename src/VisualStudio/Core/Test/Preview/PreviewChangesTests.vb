' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Preview
Imports Microsoft.VisualStudio.Text.Editor
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Preview
    Public Class PreviewChangesTests

        Private _exportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithPart(GetType(StubVsEditorAdaptersFactoryService)))

        <WpfFact>
        Public Async Function TestListStructure() As Task
            Using workspace = Await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(<text>
Class C
{
    void Foo()
    {
        $$
    }
}</text>.Value, exportProvider:=_exportProvider)
                Dim expectedItems = New List(Of Tuple(Of String, Integer)) From
                    {
                    Tuple.Create("topLevelItemName", 0),
                    Tuple.Create("test1.cs", 1),
                    Tuple.Create("insertion!", 2)
                    }

                Dim documentId = workspace.Documents.First().Id
                Dim document = workspace.CurrentSolution.GetDocument(documentId)

                Dim text = document.GetTextAsync().Result
                Dim textChange = New TextChange(New TextSpan(workspace.Documents.First().CursorPosition.Value, 0), "insertion!")
                Dim forkedDocument = document.WithText(text.WithChanges(textChange))

                Dim componentModel = New MockComponentModel(workspace.ExportProvider)

                Dim previewEngine = New PreviewEngine(
                    "Title", "helpString", "description", "topLevelItemName", CodeAnalysis.Glyph.Assembly,
                    forkedDocument.Project.Solution,
                    workspace.CurrentSolution,
                    componentModel)

                Dim outChangeList As Object = Nothing
                previewEngine.GetRootChangesList(outChangeList)
                Dim topLevelList = DirectCast(outChangeList, ChangeList)

                AssertTreeStructure(expectedItems, topLevelList)
            End Using
        End Function

        <WpfFact, WorkItem(1036455)>
        Public Async Function TestListStructure_AddedDeletedDocuments() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document FilePath="test1.cs">
Class C
{
    void Foo()
    {
        $$
    }
}                        
                        </Document>
                        <Document FilePath="test2.cs">// This file will be deleted!</Document>
                    </Project>
                </Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml, exportProvider:=_exportProvider)
                Dim expectedItems = New List(Of Tuple(Of String, Integer)) From
                    {
                    Tuple.Create("topLevelItemName", 0),
                    Tuple.Create("test1.cs", 1),
                    Tuple.Create("insertion!", 2),
                    Tuple.Create(ServicesVSResources.PreviewChangesAddedPrefix + "test3.cs", 1),
                    Tuple.Create("// This file will be added!", 2),
                    Tuple.Create(ServicesVSResources.PreviewChangesDeletedPrefix + "test2.cs", 1),
                    Tuple.Create("// This file will be deleted!", 2)
                    }

                Dim docId = workspace.Documents.First().Id
                Dim document = workspace.CurrentSolution.GetDocument(docId)

                Dim text = document.GetTextAsync().Result
                Dim textChange = New TextChange(New TextSpan(workspace.Documents.First().CursorPosition.Value, 0), "insertion!")
                Dim forkedDocument = document.WithText(text.WithChanges(textChange))
                Dim newSolution = forkedDocument.Project.Solution

                Dim removedDocumentId = workspace.Documents.Last().Id
                newSolution = newSolution.RemoveDocument(removedDocumentId)

                Dim addedDocumentId = DocumentId.CreateNewId(docId.ProjectId)
                newSolution = newSolution.AddDocument(addedDocumentId, "test3.cs", "// This file will be added!")

                Dim componentModel = New MockComponentModel(workspace.ExportProvider)

                Dim previewEngine = New PreviewEngine(
                    "Title", "helpString", "description", "topLevelItemName", CodeAnalysis.Glyph.Assembly,
                    newSolution,
                    workspace.CurrentSolution,
                    componentModel)

                Dim outChangeList As Object = Nothing
                previewEngine.GetRootChangesList(outChangeList)
                Dim topLevelList = DirectCast(outChangeList, ChangeList)

                AssertTreeStructure(expectedItems, topLevelList)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestCheckedItems() As Task
            Using workspace = Await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(<text>
Class C
{
    void Foo()
    {
        $$
    }
}</text>.Value, exportProvider:=_exportProvider)
                Dim expectedItems = New List(Of String) From {"topLevelItemName", "*test1.cs", "**insertion!"}

                Dim documentId = workspace.Documents.First().Id
                Dim document = workspace.CurrentSolution.GetDocument(documentId)

                Dim text = document.GetTextAsync().Result
                Dim textChange = New TextChange(New TextSpan(workspace.Documents.First().CursorPosition.Value, 0), "insertion!")
                Dim forkedDocument = document.WithText(text.WithChanges(textChange))

                Dim componentModel = New MockComponentModel(workspace.ExportProvider)

                Dim previewEngine = New PreviewEngine(
                    "Title", "helpString", "description", "topLevelItemName", CodeAnalysis.Glyph.Assembly,
                    forkedDocument.Project.Solution,
                    workspace.CurrentSolution,
                    componentModel)

                WpfTestCase.RequireWpfFact("Test explicitly creates an IWpfTextView")
                Dim textEditorFactory = componentModel.GetService(Of ITextEditorFactoryService)
                Using disposableView As DisposableTextView = textEditorFactory.CreateDisposableTextView()
                    previewEngine.SetTextView(disposableView.TextView)

                    Dim outChangeList As Object = Nothing
                    previewEngine.GetRootChangesList(outChangeList)
                    Dim topLevelList = DirectCast(outChangeList, ChangeList)

                    SetCheckedChildren(New List(Of String)(), topLevelList)
                    previewEngine.ApplyChanges()
                    Dim finalText = previewEngine.FinalSolution.GetDocument(documentId).GetTextAsync().Result.ToString()
                    Assert.Equal(document.GetTextAsync().Result.ToString(), finalText)
                End Using


            End Using
        End Function

        <WpfFact, WorkItem(1036455)>
        Public Async Function TestCheckedItems_AddedDeletedDocuments() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document FilePath="test1.cs">
Class C
{
    void Foo()
    {
        $$
    }
}                        
                        </Document>
                        <Document FilePath="test2.cs">// This file will be deleted!</Document>
                        <Document FilePath="test3.cs">// This file will just escape deletion!</Document>
                    </Project>
                </Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml, exportProvider:=_exportProvider)
                Dim docId = workspace.Documents.First().Id
                Dim document = workspace.CurrentSolution.GetDocument(docId)

                Dim text = document.GetTextAsync().Result
                Dim textChange = New TextChange(New TextSpan(workspace.Documents.First().CursorPosition.Value, 0), "insertion!")
                Dim forkedDocument = document.WithText(text.WithChanges(textChange))
                Dim newSolution = forkedDocument.Project.Solution

                Dim componentModel = New MockComponentModel(workspace.ExportProvider)

                Dim removedDocumentId1 = workspace.Documents.ElementAt(1).Id
                Dim removedDocumentId2 = workspace.Documents.ElementAt(2).Id
                newSolution = newSolution.RemoveDocument(removedDocumentId1)
                newSolution = newSolution.RemoveDocument(removedDocumentId2)

                Dim addedDocumentId1 = DocumentId.CreateNewId(docId.ProjectId)
                Dim addedDocumentId2 = DocumentId.CreateNewId(docId.ProjectId)
                Dim addedDocumentText = "// This file will be added!"
                newSolution = newSolution.AddDocument(addedDocumentId1, "test4.cs", addedDocumentText)
                newSolution = newSolution.AddDocument(addedDocumentId2, "test5.cs", "// This file will be unchecked and not added!")

                Dim previewEngine = New PreviewEngine(
                    "Title", "helpString", "description", "topLevelItemName", CodeAnalysis.Glyph.Assembly,
                    newSolution,
                    workspace.CurrentSolution,
                    componentModel)

                WpfTestCase.RequireWpfFact("Test explicitly creates an IWpfTextView")
                Dim textEditorFactory = componentModel.GetService(Of ITextEditorFactoryService)
                Using disposableView As DisposableTextView = textEditorFactory.CreateDisposableTextView()
                    previewEngine.SetTextView(disposableView.TextView)

                    Dim outChangeList As Object = Nothing
                    previewEngine.GetRootChangesList(outChangeList)
                    Dim topLevelList = DirectCast(outChangeList, ChangeList)

                    Dim checkedItems = New List(Of String) From
                    {
                        "test1.cs",
                        ServicesVSResources.PreviewChangesAddedPrefix + "test4.cs",
                        ServicesVSResources.PreviewChangesDeletedPrefix + "test2.cs"
                    }

                    SetCheckedChildren(checkedItems, topLevelList)
                    previewEngine.ApplyChanges()
                    Dim finalSolution = previewEngine.FinalSolution
                    Dim finalDocuments = finalSolution.Projects.First().Documents
                    Assert.Equal(3, finalDocuments.Count)

                    Dim changedDocText = finalSolution.GetDocument(docId).GetTextAsync().Result.ToString()
                    Assert.Equal(forkedDocument.GetTextAsync().Result.ToString(), changedDocText)

                    Dim finalAddedDocText = finalSolution.GetDocument(addedDocumentId1).GetTextAsync().Result.ToString()
                    Assert.Equal(addedDocumentText, finalAddedDocText)

                    Dim finalNotRemovedDocText = finalSolution.GetDocument(removedDocumentId2).GetTextAsync().Result.ToString()
                    Assert.Equal("// This file will just escape deletion!", finalNotRemovedDocText)
                End Using
            End Using
        End Function

        <WpfFact>
        Public Async Function TestLinkedFileChangesMergedAndDeduplicated() As Task

            Dim workspaceXml = <Workspace>
                                   <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj1">
                                       <Document FilePath="C.vb"><![CDATA[
Class C
    Sub M()
    End Sub

    Sub X()
    End Sub()
End Class
]]>
                                       </Document>
                                   </Project>
                                   <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj2">
                                       <Document IsLinkFile="true" LinkAssemblyName="VBProj1" LinkFilePath="C.vb"/>
                                   </Project>
                               </Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceXml, , exportProvider:=_exportProvider)
                Dim documentId1 = workspace.Documents.Where(Function(d) d.Project.Name = "VBProj1").Single().Id
                Dim document1 = workspace.CurrentSolution.GetDocument(documentId1)

                Dim documentId2 = workspace.Documents.Where(Function(d) d.Project.Name = "VBProj2").Single().Id
                Dim document2 = workspace.CurrentSolution.GetDocument(documentId2)

                Dim text1 = document1.GetTextAsync().Result
                Dim textChange1 = New TextChange(New TextSpan(19, 1), "N")

                Dim text2 = document2.GetTextAsync().Result
                Dim textChange2 = New TextChange(New TextSpan(47, 1), "Y")

                Dim updatedSolution = document1.Project.Solution _
                    .WithDocumentText(documentId1, text1.WithChanges(textChange1)) _
                    .WithDocumentText(documentId2, text2.WithChanges(textChange2))

                Dim componentModel = New MockComponentModel(workspace.ExportProvider)

                Dim previewEngine = New PreviewEngine(
                    "Title", "helpString", "description", "topLevelItemName", CodeAnalysis.Glyph.Assembly,
                    updatedSolution,
                    workspace.CurrentSolution,
                    componentModel)

                Dim outChangeList As Object = Nothing
                previewEngine.GetRootChangesList(outChangeList)
                Dim topLevelList = DirectCast(outChangeList, ChangeList)

                Dim expectedItems = New List(Of Tuple(Of String, Integer)) From
                    {
                        Tuple.Create("topLevelItemName", 0),
                        Tuple.Create("C.vb", 1),
                        Tuple.Create("Sub N()", 2),
                        Tuple.Create("Sub Y()", 2)
                    }

                AssertTreeStructure(expectedItems, topLevelList)
            End Using
        End Function

        Private Sub AssertTreeStructure(expectedItems As List(Of Tuple(Of String, Integer)), topLevelList As ChangeList)
            Dim outChangeList As Object = Nothing
            Dim outCanRecurse As Integer = Nothing
            Dim outTreeList As Shell.Interop.IVsLiteTreeList = Nothing

            Dim flatteningResult = New List(Of Tuple(Of String, Integer))()
            FlattenTree(topLevelList, flatteningResult, 0)

            Assert.Equal(expectedItems.Count, flatteningResult.Count)

            For x As Integer = 0 To flatteningResult.Count - 1
                Assert.Equal(flatteningResult(x), expectedItems(x))
            Next
        End Sub

        Private Sub FlattenTree(list As ChangeList, result As List(Of Tuple(Of String, Integer)), depth As Integer)
            For Each change In list.Changes
                Dim text As String = Nothing
                change.GetText(Nothing, text)
                result.Add(Tuple.Create(text, depth))

                If change.Children.Changes.Any Then
                    FlattenTree(change.Children, result, depth + 1)
                End If
            Next
        End Sub

        ' Check each of the most-nested children whose names appear in checkItems.
        ' Uncheck the rest
        Private Sub SetCheckedChildren(checkedItems As List(Of String), topLevelList As ChangeList)
            For Each change In topLevelList.Changes
                Dim text As String = Nothing
                change.GetText(Nothing, text)
                Dim isChecked = change.CheckState = Shell.Interop.__PREVIEWCHANGESITEMCHECKSTATE.PCCS_Checked
                If checkedItems.Contains(text) Then
                    If Not isChecked Then
                        change.Toggle()
                    End If

                    Continue For
                ElseIf isChecked Then
                    change.Toggle()
                End If

                If change.Children.Changes.Any() Then
                    SetCheckedChildren(checkedItems, change.Children)
                End If
            Next
        End Sub

        Private Sub AssertChildCount(list As ChangeList, count As UInteger)
            Dim actualCount As UInteger = Nothing
            list.GetItemCount(actualCount)
            Assert.Equal(count, actualCount)
        End Sub

        Private Sub AssertChildText(list As ChangeList, index As UInteger, text As String)
            Dim actualText As String = Nothing
            list.GetText(index, Nothing, actualText)
            Assert.Equal(text, actualText)
        End Sub

        Private Sub AssertSomeChild(list As ChangeList, text As String)
            Dim count As UInteger = Nothing
            list.GetItemCount(count)
            For i As UInteger = 0 To count
                Dim actualText As String = Nothing
                list.GetText(i, Nothing, actualText)

                If actualText = text Then
                    Return
                End If
            Next

            Assert.True(False, "Didn't find child with name '" + text + "'")
        End Sub

    End Class
End Namespace
