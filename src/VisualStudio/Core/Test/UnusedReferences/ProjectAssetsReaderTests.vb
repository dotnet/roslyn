' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.UnusedReferences
Imports Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.ProjectAssets

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.UnusedReferences
    Public Class ProjectAssetsReaderTests
        Private Const TargetFramework = ".NETCoreApp,Version=v3.1"
        Private Const Version3 = 3

        <Theory, Trait(Traits.Feature, Traits.Features.Packaging)>
        <InlineData(0)>
        <InlineData(1)>
        <InlineData(2)>
        <InlineData(4)>
        <InlineData(5)>
        Public Sub NoReferencesReadWhenProjectAssetsVersionNot3(version As Integer)
            Dim myPackage = PackageReference("MyPackage.dll")
            Dim references = ImmutableArray.Create(myPackage)
            Dim projectAssets = TestProjectAssetsFile.Create(version, TargetFramework, references)

            Dim realizedReferences = ProjectAssetsReader.ReadReferences(references, projectAssets)

            Assert.Empty(realizedReferences)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Sub ReferencesReadWhenProjectAssetsVersionIs3()
            Dim myPackage = PackageReference("MyPackage.dll")
            Dim references = ImmutableArray.Create(myPackage)
            Dim projectAssets = TestProjectAssetsFile.Create(Version3, TargetFramework, references)

            Dim realizedReferences = ProjectAssetsReader.ReadReferences(references, projectAssets)

            Dim realizedReference = Assert.Single(realizedReferences)
            Assert.Equal(myPackage.ItemSpecification, realizedReference.ItemSpecification)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Sub ReferenceNotReadWhenReferenceNotPresent()
            Dim references = ImmutableArray.Create(PackageReference("MyPackage.dll"))
            Dim projectAssets = TestProjectAssetsFile.Create(Version3, TargetFramework, references)

            Dim differentReference = ImmutableArray.Create(ProjectReference("MyProject.csproj"))

            Dim realizedReferences = ProjectAssetsReader.ReadReferences(differentReference, projectAssets)

            Assert.Empty(realizedReferences)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Packaging)>
        Public Sub ProjectReferencesReadHaveTheirPathAsTheItemSpecification()
            Dim mylibraryPath = ".\Library\MyLibrary.csproj"
            Dim references = ImmutableArray.Create(ProjectReference(mylibraryPath))
            Dim projectAssets = TestProjectAssetsFile.Create(Version3, TargetFramework, references)

            Dim realizedReferences = ProjectAssetsReader.ReadReferences(references, projectAssets)

            Dim realizedReference = Assert.Single(realizedReferences)
            Assert.Equal(mylibraryPath, realizedReference.ItemSpecification)
        End Sub

        Private Shared Function ProjectReference(projectPath As String, ParamArray dependencies As ReferenceInfo()) As ReferenceInfo
            Return ProjectReference(projectPath, False, dependencies)
        End Function
        Private Shared Function ProjectReference(projectPath As String, treatAsUsed As Boolean, ParamArray dependencies As ReferenceInfo()) As ReferenceInfo
            Return New ReferenceInfo(ReferenceType.Project,
                projectPath,
                treatAsUsed,
                ImmutableArray.Create(Path.ChangeExtension(projectPath, "dll")),
                dependencies.ToImmutableArray())
        End Function

        Private Shared Function PackageReference(assemblyPath As String, ParamArray dependencies As ReferenceInfo()) As ReferenceInfo
            Return PackageReference(assemblyPath, False, dependencies)
        End Function
        Private Shared Function PackageReference(assemblyPath As String, treatAsUsed As Boolean, ParamArray dependencies As ReferenceInfo()) As ReferenceInfo
            Return New ReferenceInfo(ReferenceType.Package,
                Path.GetFileNameWithoutExtension(assemblyPath),
                treatAsUsed,
                ImmutableArray.Create(assemblyPath),
                dependencies.ToImmutableArray())
        End Function
    End Class
End Namespace
