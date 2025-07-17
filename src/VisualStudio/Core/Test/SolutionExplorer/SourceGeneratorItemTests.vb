' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.Internal.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Diagnostics)>
    Public Class SourceGeneratorItemTests
        <WpfFact>
        Public Async Function SourceGeneratorsListed() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id

                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Assert.True(source.HasItems)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))

                Assert.Equal(GetType(GenerateFileForEachAdditionalFileWithContentsCommented).FullName, generatorItem.Text)
            End Using
        End Function

        <WpfFact>
        Public Async Function PlaceholderItemCreateIfGeneratorProducesNoFiles() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Await WaitForGeneratorsAndItemSourcesAsync(workspace)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Dim generatorFilesItemSource = CreateSourceGeneratedFilesItemSource(workspace, generatorItem)

                ' We have items even before we expand, but then must expand to get real items
                Assert.True(generatorFilesItemSource.HasItems)
                Assert.IsAssignableFrom(Of ISupportExpansionEvents)(generatorFilesItemSource).BeforeExpand()

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Assert.IsType(Of NoSourceGeneratedFilesPlaceholderItem)(Assert.Single(generatorFilesItemSource.Items))
            End Using
        End Function

        <WpfFact>
        Public Async Function SingleSourceGeneratedFileProducesItem() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
                        <AdditionalDocument FilePath="Test.txt"></AdditionalDocument>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Await WaitForGeneratorsAndItemSourcesAsync(workspace)
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

        <WpfFact>
        Public Async Function MultipleSourceGeneratedFilesProducesSortedItem() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
                        <AdditionalDocument FilePath="Test1.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test2.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test3.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test4.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test5.txt"></AdditionalDocument>
                        <AdditionalDocument FilePath="Test6.txt"></AdditionalDocument>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Await WaitForGeneratorsAndItemSourcesAsync(workspace)
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

        <WpfTheory, CombinatorialData>
        Friend Async Function ChangeToRemoveAllGeneratedDocumentsUpdatesListCorrectly(
                preference As SourceGeneratorExecutionPreference) As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
                        <AdditionalDocument FilePath="Test1.txt"></AdditionalDocument>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(
                    workspaceXml,
                    composition:=EditorTestCompositions.EditorFeatures.AddParts(GetType(TestWorkspaceConfigurationService)))

                Dim configService = workspace.ExportProvider.GetExportedValue(Of TestWorkspaceConfigurationService)
                configService.Options = New WorkspaceConfigurationOptions(SourceGeneratorExecution:=preference)

                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Await WaitForGeneratorsAndItemSourcesAsync(workspace)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Dim generatorFilesItemSource = CreateSourceGeneratedFilesItemSource(workspace, generatorItem)

                Assert.IsAssignableFrom(Of ISupportExpansionEvents)(generatorFilesItemSource).BeforeExpand()

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Assert.IsType(Of SourceGeneratedFileItem)(Assert.Single(generatorFilesItemSource.Items))

                workspace.OnAdditionalDocumentRemoved(workspace.CurrentSolution.GetProject(projectId).AdditionalDocumentIds.Single())

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                ' In balanced-mode the SG file won't go away until a save/build happens.
                If preference = SourceGeneratorExecutionPreference.Automatic Then
                    Assert.IsType(Of NoSourceGeneratedFilesPlaceholderItem)(Assert.Single(generatorFilesItemSource.Items))
                Else
                    Assert.IsType(Of SourceGeneratedFileItem)(Assert.Single(generatorFilesItemSource.Items))
                End If
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Friend Async Function AddingAGeneratedDocumentUpdatesListCorrectly(
                preference As SourceGeneratorExecutionPreference) As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(
                    workspaceXml,
                    composition:=EditorTestCompositions.EditorFeatures.AddParts(GetType(TestWorkspaceConfigurationService)))

                Dim configService = workspace.ExportProvider.GetExportedValue(Of TestWorkspaceConfigurationService)
                configService.Options = New WorkspaceConfigurationOptions(SourceGeneratorExecution:=preference)

                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Await WaitForGeneratorsAndItemSourcesAsync(workspace)
                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Dim generatorFilesItemSource = CreateSourceGeneratedFilesItemSource(workspace, generatorItem)

                Assert.IsAssignableFrom(Of ISupportExpansionEvents)(generatorFilesItemSource).BeforeExpand()

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Assert.IsType(Of NoSourceGeneratedFilesPlaceholderItem)(Assert.Single(generatorFilesItemSource.Items))

                ' Add a first item and see if it updates correctly
                workspace.OnAdditionalDocumentAdded(
                    DocumentInfo.Create(
                        DocumentId.CreateNewId(projectId),
                        "Test.txt"))

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                ' In balanced-mode the SG file won't be created until a save/build happens.
                If preference = SourceGeneratorExecutionPreference.Automatic Then
                    Assert.IsType(Of SourceGeneratedFileItem)(Assert.Single(generatorFilesItemSource.Items))
                Else
                    Assert.IsType(Of NoSourceGeneratedFilesPlaceholderItem)(Assert.Single(generatorFilesItemSource.Items))
                End If

                ' Add a second item and see if it updates correctly again
                workspace.OnAdditionalDocumentAdded(
                    DocumentInfo.Create(
                        DocumentId.CreateNewId(projectId),
                        "Test2.txt"))

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                If preference = SourceGeneratorExecutionPreference.Automatic Then
                    Assert.Equal(2, generatorFilesItemSource.Items.Cast(Of SourceGeneratedFileItem)().Count())
                Else
                    Assert.Equal(1, generatorFilesItemSource.Items.Cast(Of NoSourceGeneratedFilesPlaceholderItem)().Count())
                End If
            End Using
        End Function

        <WpfFact>
        Public Async Function GeneratedDocumentsStayingTheSameWorksCorrectly() As Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" LanguageVersion="Preview">
                        <AdditionalDocument FilePath="Test1.txt"></AdditionalDocument>
                    </Project>
                </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceXml)
                Dim projectId = workspace.Projects.Single().Id
                Dim source = CreateItemSourceForAnalyzerReference(workspace, projectId)
                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Dim generatorItem = Assert.IsAssignableFrom(Of SourceGeneratorItem)(Assert.Single(source.Items))
                Dim generatorFilesItemSource = CreateSourceGeneratedFilesItemSource(workspace, generatorItem)

                Assert.IsAssignableFrom(Of ISupportExpansionEvents)(generatorFilesItemSource).BeforeExpand()

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Dim itemBeforeUpdate = Assert.IsType(Of SourceGeneratedFileItem)(Assert.Single(generatorFilesItemSource.Items))

                ' Change a document; this will produce updated documents but no new hint path is being introduced or removed
                workspace.OnAdditionalDocumentTextChanged(workspace.CurrentSolution.Projects.Single().AdditionalDocumentIds.Single(),
                    SourceText.From("Changed"),
                    PreservationMode.PreserveValue)

                Await WaitForGeneratorsAndItemSourcesAsync(workspace)

                Dim itemAfterUpdate = Assert.IsType(Of SourceGeneratedFileItem)(Assert.Single(generatorFilesItemSource.Items))

                Assert.Same(itemBeforeUpdate, itemAfterUpdate)
            End Using
        End Function

        Private Shared Function CreateItemSourceForAnalyzerReference(workspace As EditorTestWorkspace, projectId As ProjectId) As BaseDiagnosticAndGeneratorItemSource
            Dim analyzerReference = New TestGeneratorReference(New GenerateFileForEachAdditionalFileWithContentsCommented())
            workspace.OnAnalyzerReferenceAdded(projectId, analyzerReference)

            Return New LegacyDiagnosticItemSource(
                workspace.GetService(Of IThreadingContext),
                New AnalyzerItem(New AnalyzersFolderItem(workspace.GetService(Of IThreadingContext), workspace, projectId, Nothing, Nothing), analyzerReference, Nothing),
                New FakeAnalyzersCommandHandler,
                workspace.GetService(Of IAsynchronousOperationListenerProvider))
        End Function

        Private Shared Function CreateSourceGeneratedFilesItemSource(workspace As EditorTestWorkspace, generatorItem As SourceGeneratorItem) As Shell.IAttachedCollectionSource
            Dim asyncListener = workspace.GetService(Of IAsynchronousOperationListenerProvider).GetListener(FeatureAttribute.SourceGenerators)

            Return New SourceGeneratedFileItemSource(generatorItem, workspace.GetService(Of IThreadingContext), workspace, asyncListener)
        End Function

        Private Shared Function WaitForGeneratorsAndItemSourcesAsync(workspace As EditorTestWorkspace) As Task
            Dim service = workspace.GetService(Of AsynchronousOperationListenerProvider)

            ' We wait for the Workspace to ensure that any WorkspaceChanged events have been raised; we wait for SourceGenerators
            ' as that is what refreshes of the list are registered against.
            Return service.WaitAllAsync(workspace, (New String() {FeatureAttribute.Workspace, FeatureAttribute.SourceGenerators}))
        End Function
    End Class
End Namespace
