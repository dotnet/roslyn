' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.Internal.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Microsoft.VisualStudio.Shell
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Diagnostics)>
    Public Class AnalyzersFolderProviderTests
        <WpfFact>
        Public Sub CreateCollectionSource_NullItem()
            Using environment = New TestEnvironment()
                Dim provider As IAttachedCollectionSourceProvider =
                    New AnalyzersFolderItemSourceProvider(environment.ExportProvider.GetExportedValue(Of IThreadingContext), environment.Workspace, Nothing)

                Dim collectionSource = provider.CreateCollectionSource(Nothing, KnownRelationships.Contains)

                Assert.Null(collectionSource)
            End Using
        End Sub

        <WpfFact>
        Public Sub CreateCollectionSource_NullHierarchyIdentity()
            Using environment = New TestEnvironment()
                Dim provider As IAttachedCollectionSourceProvider =
                    New AnalyzersFolderItemSourceProvider(environment.ExportProvider.GetExportedValue(Of IThreadingContext), environment.Workspace, Nothing)

                Dim hierarchyItem = New MockHierarchyItem With {.HierarchyIdentity = Nothing}

                Dim collectionSource = provider.CreateCollectionSource(hierarchyItem, KnownRelationships.Contains)

                Assert.Null(collectionSource)
            End Using
        End Sub

        <WpfFact>
        Public Sub CreateCollectionSource()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Goo")
                Dim hierarchy = project.Hierarchy

                Dim hierarchyItem = New MockHierarchyItem With {
                    .HierarchyIdentity = New MockHierarchyItemIdentity With {
                        .NestedHierarchy = hierarchy,
                        .NestedItemID = MockHierarchy.ReferencesNodeItemId
                    },
                    .CanonicalName = "References",
                    .Parent = New MockHierarchyItem With {
                        .HierarchyIdentity = New MockHierarchyItemIdentity With {
                            .NestedHierarchy = hierarchy,
                            .NestedItemID = VSConstants.VSITEMID.Root
                        },
                        .CanonicalName = "Goo"
                    }
                }

                Dim provider As IAttachedCollectionSourceProvider = New AnalyzersFolderItemSourceProvider(
                    environment.ExportProvider.GetExportedValue(Of IThreadingContext), environment.Workspace, New FakeAnalyzersCommandHandler)

                Dim collectionSource = provider.CreateCollectionSource(hierarchyItem, KnownRelationships.Contains)

                Assert.NotNull(collectionSource)

                Dim items = TryCast(collectionSource.Items, ObservableCollection(Of AnalyzersFolderItem))

                Assert.Equal(expected:=1, actual:=items.Count)

            End Using
        End Sub
    End Class
End Namespace

