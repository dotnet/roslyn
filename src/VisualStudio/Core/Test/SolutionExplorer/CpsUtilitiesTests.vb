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
        Public Sub ExtractAnalyzerFilePath_MalformedCanonicalName()
            Dim projectDirectoryFullPath = "C:\users\me\Solution\Project"
            Dim analyzerCanonicalName = "alpha beta gamma"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Null(analyzerFileFullPath)
        End Sub
    End Class

End Namespace

