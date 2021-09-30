﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.Internal.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <UseExportProvider>
    Public Class SourceGeneratorItemTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub SourceGeneratorsListed()
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id

                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)

                Assert.True(source.HasItems)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))

                Assert.Equal(GetType(GenerateFileForEachAdditionalFileWithContentsCommented).FullName, generatorItem.Text)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function PlaceholderItemCreateIfGeneratorProducesNoFiles() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Dim generatorFilesItemSource = CreateSourceGeneratedFilesItemSource(workspace, generatorItem)

                ' We have items even before we expand, but then must expand to get real items
                Assert.True(generatorFilesItemSource.HasItems)
                Assert.IsAssignableFrom(Of ISupportExpansionEvents)(generatorFilesItemSource).BeforeExpand()

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Assert.IsType(Of NoSourceGeneratedFilesPlaceholderItem)(Assert.Single(generatorFilesItemSource.Items))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function SingleSourceGeneratedFileProducesItem() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <AdditionalDocument FilePath="Test.txt"></AdditionalDocument>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Dim generatorFilesItemSource = CreateSourceGeneratedFilesItemSource(workspace, generatorItem)

                ' We have items even before we expand, but then must expand to get real items
                Assert.True(generatorFilesItemSource.HasItems)
                Assert.IsAssignableFrom(Of ISupportExpansionEvents)(generatorFilesItemSource).BeforeExpand()

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Dim fileItem = Assert.IsType(Of SourceGeneratedFileItem)(Assert.Single(generatorFilesItemSource.Items))
                Dim sourceGeneratedDocument = Assert.Single(Await workspace.CurrentSolution.GetProject(projectId).GetSourceGeneratedDocumentsAsync())

                Assert.Equal(sourceGeneratedDocument.Name, fileItem.Text)
                Assert.Equal(sourceGeneratedDocument.Id, fileItem.DocumentId)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function MultipleSourceGeneratedFilesProducesSortedItem() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <AdditionalDocument FilePath="Test1.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test2.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test3.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test4.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test5.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test6.txt"></AdditionalDocument>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Dim generatorFilesItemSource = CreateSourceGeneratedFilesItemSource(workspace, generatorItem)

                Assert.IsAssignableFrom(Of ISupportExpansionEvents)(generatorFilesItemSource).BeforeExpand()

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Dim expectedNames = Aggregate document In workspace.CurrentSolution.GetProject(projectId).AdditionalDocuments
                                    Select document.Name.Replace(".txt", ".generated.cs")
                                    Into ToList()

                Assert.Equal(expectedNames, generatorFilesItemSource.Items.Cast(Of SourceGeneratedFileItem).Select(Function(i) i.Text))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function ChangeToNoGeneratedDocumentsUpdatesListCorrectly() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <AdditionalDocument FilePath="Test1.txt"></AdditionalDocument>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Dim generatorFilesItemSource = CreateSourceGeneratedFilesItemSource(workspace, generatorItem)

                Assert.IsAssignableFrom(Of ISupportExpansionEvents)(generatorFilesItemSource).BeforeExpand()

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Assert.IsType(Of SourceGeneratedFileItem)(Assert.Single(generatorFilesItemSource.Items))

                workspace.OnAdditionalDocumentRemoved(workspace.CurrentSolution.GetProject(projectId).AdditionalDocumentIds.Single())

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Assert.IsType(Of NoSourceGeneratedFilesPlaceholderItem)(Assert.Single(generatorFilesItemSource.Items))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Async Function AddingAGeneratedDocumentUpdatesListCorrectly() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Dim generatorFilesItemSource = CreateSourceGeneratedFilesItemSource(workspace, generatorItem)

                Assert.IsAssignableFrom(Of ISupportExpansionEvents)(generatorFilesItemSource).BeforeExpand()

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Assert.IsType(Of NoSourceGeneratedFilesPlaceholderItem)(Assert.Single(generatorFilesItemSource.Items))

                workspace.OnAdditionalDocumentAdded(
                    DocumentInfo.Create(
                        DocumentId.CreateNewId(projectId),
                        "Test.txt"))

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Assert.IsType(Of SourceGeneratedFileItem)(Assert.Single(generatorFilesItemSource.Items))
            End Using
        End Function

        Private Shared Function CreateItemSourceForAnalyzerReference(workspace As TestWorkspace, projectId As ProjectId) As BaseDiagnosticAndGeneratorItemSource
            Dim analyzerReference = New TestGeneratorReference(New GenerateFileForEachAdditionalFileWithContentsCommented())
            workspace.OnAnalyzerReferenceAdded(projectId, analyzerReference)

            Return New LegacyDiagnosticItemSource(
                New AnalyzerItem(New AnalyzersFolderItem(workspace, projectId, Nothing, Nothing), analyzerReference, Nothing),
                New FakeAnalyzersCommandHandler,
                workspace.GetService(Of IDiagnosticAnalyzerService))
        End Function

        Private Shared Function CreateSourceGeneratedFilesItemSource(workspace As TestWorkspace, generatorItem As SourceGeneratorItem) As Shell.IAttachedCollectionSource
            Dim asyncListener = workspace.GetService(Of IAsynchronousOperationListenerProvider).GetListener(FeatureAttribute.SourceGenerators)

            Return New SourceGeneratedFileItemSource(generatorItem, workspace, asyncListener, workspace.GetService(Of IThreadingContext)())
        End Function

        Private Shared Function WaitForGeneratorsAndItemSourcesAsync(workspace As TestWorkspace) As Task
            Dim service = workspace.GetService(Of AsynchronousOperationListenerProvider)

            ' We wait for the Workspace to ensure that any WorkspaceChanged events have been raised; we wait for SourceGenerators
            ' as that is what refreshes of the list are registered against.
            Return service.WaitAllAsync(workspace, (New String() {FeatureAttribute.Workspace, FeatureAttribute.SourceGenerators}))
        End Function
    End Class
End Namespace
