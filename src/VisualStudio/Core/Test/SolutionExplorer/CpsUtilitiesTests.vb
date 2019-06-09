' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer

    Public Class CpsUtilitiesTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub ExtractAnalyzerFilePath_WithProjectPath()
            Dim projectDirectoryFullPath = "C:\users\me\Solution\Project"
            Dim analyzerCanonicalName = "C:\users\me\Solution\Project\netstandard2.0\analyzerdependency\C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Equal(expected:="C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll", actual:=analyzerFileFullPath)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub ExtractAnalyzerFilePath_WithoutProjectPath()
            Dim projectDirectoryFullPath = "C:\users\me\Solution\Project"
            Dim analyzerCanonicalName = "netstandard2.0\analyzerdependency\C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Equal(expected:="C:\users\me\.nuget\package\analyzer\MyAnalyzer.dll", actual:=analyzerFileFullPath)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub ExtractAnalyzerFilePath_MalformedCanonicalName()
            Dim projectDirectoryFullPath = "C:\users\me\Solution\Project"
            Dim analyzerCanonicalName = "alpha beta gamma"

            Dim analyzerFileFullPath = CpsUtilities.ExtractAnalyzerFilePath(projectDirectoryFullPath, analyzerCanonicalName)
            Assert.Null(analyzerFileFullPath)
        End Sub
    End Class

End Namespace

