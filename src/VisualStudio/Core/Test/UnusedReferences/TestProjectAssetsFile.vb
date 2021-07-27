' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis.UnusedReferences
Imports Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.ProjectAssets

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnusedReferences
    Friend Module TestProjectAssetsFile
        Public Function Create(
            version As Integer,
            targetFramework As String,
            references As ImmutableArray(Of ReferenceInfo)) As ProjectAssetsFile

            Dim allReferences = New List(Of ReferenceInfo)
            FlattenReferences(references, allReferences)

            Dim libraries = BuildLibraries(allReferences)
            Dim targets = BuildTargets(targetFramework, allReferences)
            Dim project = BuildProject(targetFramework)

            Dim projectAssets As ProjectAssetsFile = New ProjectAssetsFile With {
                .Version = version,
                .Targets = targets,
                .Libraries = libraries,
                .Project = project
            }

            Return projectAssets
        End Function

        Private Sub FlattenReferences(references As ImmutableArray(Of ReferenceInfo), allReferences As List(Of ReferenceInfo))
            For Each reference In references
                FlattenReference(reference, allReferences)
            Next
        End Sub

        Private Sub FlattenReference(reference As ReferenceInfo, allReferences As List(Of ReferenceInfo))
            allReferences.Add(reference)
            FlattenReferences(reference.Dependencies, allReferences)
        End Sub

        Private Function BuildLibraries(references As List(Of ReferenceInfo)) As Dictionary(Of String, ProjectAssetsLibrary)
            Dim libraries = New Dictionary(Of String, ProjectAssetsLibrary)

            For Each reference In references
                Dim library = New ProjectAssetsLibrary With {
                    .Path = reference.ItemSpecification
                }
                libraries.Add(Path.GetFileNameWithoutExtension(library.Path), library)
            Next

            Return libraries
        End Function

        Private Function BuildTargets(targetFramework As String, references As List(Of ReferenceInfo)) As Dictionary(Of String, Dictionary(Of String, ProjectAssetsTargetLibrary))
            Dim libraries = New Dictionary(Of String, ProjectAssetsTargetLibrary)

            For Each reference In references
                Dim dependencies = BuildDependencies(reference.Dependencies)
                Dim library = New ProjectAssetsTargetLibrary With {
                    .Type = GetLibraryType(reference.ReferenceType),
                    .Compile = New Dictionary(Of String, ProjectAssetsTargetLibraryCompile) From {
                        {Path.ChangeExtension(reference.ItemSpecification, "dll"), Nothing}
                    },
                    .Dependencies = dependencies
                }
                libraries(Path.GetFileNameWithoutExtension(reference.ItemSpecification)) = library
            Next

            Return New Dictionary(Of String, Dictionary(Of String, ProjectAssetsTargetLibrary)) From {
                {targetFramework, libraries}
            }
        End Function

        Private Function GetLibraryType(referenceType As ReferenceType) As String
            If referenceType = ReferenceType.Package Then Return "package"
            If referenceType = ReferenceType.Project Then Return "project"
            Return "assembly"
        End Function

        Private Function BuildDependencies(references As ImmutableArray(Of ReferenceInfo)) As Dictionary(Of String, String)
            Return references.ToDictionary(
                Function(reference)
                    Return Path.GetFileNameWithoutExtension(reference.ItemSpecification)
                End Function,
                Function(reference)
                    Return String.Empty
                End Function)
        End Function

        Private Function BuildProject(targetFramework As String) As ProjectAssetsProject
            ' Frameworks won't always specify a set of dependencies.
            ' This ensures the project asset reader does not error in these cases.
            Return New ProjectAssetsProject With {
                 .Frameworks = New Dictionary(Of String, ProjectAssetsProjectFramework) From {
                    {targetFramework, New ProjectAssetsProjectFramework}
                 }
            }
        End Function
    End Module
End Namespace
