' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <Trait(Traits.Feature, Traits.Features.Diagnostics)>
    Public Class CpsUtilitiesTests
        <Fact>
        Public Sub ExtractAnalyzerFilePath_WithProjectPath()
            Dim projectDirectoryFullPath = "C:\users\me\Solution\Project"
            Dim analyzerCanonicalName = "C:\users\me\Solution\Project\netstandard2.0\analyzerdependency\C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Equal(expected:="C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll", actual:=analyzerFileFullPath)
        End Sub

        <Fact>
        Public Sub ExtractAnalyzerFilePath_WithoutProjectPath_WithTfmAndProviderType()
            Dim projectDirectoryFullPath = "C:\users\me\Solution\Project"
            Dim analyzerCanonicalName = "netstandard2.0\analyzerdependency\C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Equal(expected:="C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll", actual:=analyzerFileFullPath)
        End Sub

        <Fact>
        Public Sub ExtractAnalyzerFilePath_WithoutProjectPath_WithoutTfmAndProviderType()
            Dim projectDirectoryFullPath = "C:\users\me\Solution\Project"
            Dim analyzerCanonicalName = "C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Equal(expected:="C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll", actual:=analyzerFileFullPath)
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50317")>
        Public Sub ExtractAnalyzerFilePath_WithoutProjectPath_WithoutTfmAndProviderType_SiblingFolder()
            Dim projectDirectoryFullPath = "C:\Project"
            Dim analyzerCanonicalName = "C:\Project.Analyzer\bin\Debug\MyAnalyzer.dll"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Equal(expected:="C:\Project.Analyzer\bin\Debug\MyAnalyzer.dll", actual:=analyzerFileFullPath)
        End Sub

        <Fact>
        Public Sub ExtractAnalyzerFilePath_NewerForm_AnalyzerUnderProjectDirectory()
            ' Newer-form (VS16.7+) canonical name where the analyzer lives physically under the project
            ' directory (e.g. <Analyzer Include="Analyzers\MinimalGenerator.dll" />). The canonical name
            ' is already the full file path and must be returned unchanged so it matches
            ' AnalyzerReference.FullPath. Previously, the project-directory prefix was stripped, leaving
            ' a relative path that never matched, preventing source-generator child nodes from attaching
            ' under the analyzer in Solution Explorer.
            Dim projectDirectoryFullPath = "C:\Repro\Consumer.Inside"
            Dim analyzerCanonicalName = "C:\Repro\Consumer.Inside\Analyzers\MinimalGenerator.dll"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Equal(expected:="C:\Repro\Consumer.Inside\Analyzers\MinimalGenerator.dll", actual:=analyzerFileFullPath)
        End Sub

        <Fact>
        Public Sub ExtractAnalyzerFilePath_NewerForm_AnalyzerUnderProjectDirectory_CoincidentalAnalyzerDependencySegment()
            ' Pathological newer-form case: the analyzer lives under the project directory at a location
            ' whose intermediate folder structure happens to mimic the legacy "{tfm}\analyzerdependency\..."
            ' shape after stripping the project prefix. The candidate extracted from the legacy path is not
            ' rooted ("Foo.dll"), so we fall back to the newer-form interpretation and return the original
            ' canonical name.
            Dim projectDirectoryFullPath = "C:\Proj"
            Dim analyzerCanonicalName = "C:\Proj\netstandard2.0\analyzerdependency\Foo.dll"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Equal(expected:="C:\Proj\netstandard2.0\analyzerdependency\Foo.dll", actual:=analyzerFileFullPath)
        End Sub

        <Fact>
        Public Sub ExtractAnalyzerFilePath_MalformedCanonicalName()
            Dim projectDirectoryFullPath = "C:\users\me\Solution\Project"
            Dim analyzerCanonicalName = "alpha beta gamma"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Null(analyzerFileFullPath)
        End Sub
    End Class

End Namespace

