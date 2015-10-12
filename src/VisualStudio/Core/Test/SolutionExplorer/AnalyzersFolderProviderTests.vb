' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Windows.Media
Imports Microsoft.Internal.VisualStudio.PlatformUI
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Microsoft.VisualStudio.Shell
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class AnalyzersFolderProviderTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub CreateCollectionSource_NullItem()
            Using environment = New TestEnvironment()
                Dim provider As IAttachedCollectionSourceProvider =
                New AnalyzersFolderItemProvider(environment.ServiceProvider, Nothing)

                Dim collectionSource = provider.CreateCollectionSource(Nothing, KnownRelationships.Contains)

                Assert.Null(collectionSource)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub CreateCollectionSource_NullHierarchyIdentity()
            Using environment = New TestEnvironment()
                Dim provider As IAttachedCollectionSourceProvider =
                New AnalyzersFolderItemProvider(environment.ServiceProvider, Nothing)

                Dim hierarchyItem = New MockHierarchyItem With {.HierarchyIdentity = Nothing}

                Dim collectionSource = provider.CreateCollectionSource(Nothing, KnownRelationships.Contains)

                Assert.Null(collectionSource)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub CreateCollectionSource()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Foo")
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
                        .CanonicalName = "Foo"
                    }
                }

                Dim mapper = New HierarchyItemMapper(environment.ProjectTracker)

                Dim provider As IAttachedCollectionSourceProvider = New AnalyzersFolderItemProvider(mapper, environment.Workspace, New FakeAnalyzersCommandHandler)

                Dim collectionSource = provider.CreateCollectionSource(hierarchyItem, KnownRelationships.Contains)

                Assert.NotNull(collectionSource)

                Dim items = TryCast(collectionSource.Items, ObservableCollection(Of AnalyzersFolderItem))

                Assert.Equal(expected:=1, actual:=items.Count)

            End Using
        End Sub
    End Class
End Namespace

