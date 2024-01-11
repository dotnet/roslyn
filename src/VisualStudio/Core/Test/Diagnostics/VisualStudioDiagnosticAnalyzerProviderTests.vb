' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class VisualStudioDiagnosticAnalyzerProviderTests
        <Fact>
        Public Sub GetAnalyzerReferencesInExtensions_Substitution()
            Dim extensionManager = New VisualStudioDiagnosticAnalyzerProvider(
                New MockExtensionManager({({"$RootFolder$\test\test.dll", "$ShellFolder$\test\test.dll", "test\test.dll"}, "Vsix")}),
                GetType(MockExtensionManager.MockContent))

            Dim references = extensionManager.GetAnalyzerReferencesInExtensions()

            AssertEx.SetEqual(
            {
                Path.Combine(TempRoot.Root, "ResolvedRootFolder\test\test.dll"),
                Path.Combine(TempRoot.Root, "ResolvedShellFolder\test\test.dll"),
                Path.Combine(TempRoot.Root, "InstallPath\test\test.dll")
            },
            references.Select(Function(referenceAndId) referenceAndId.reference.FullPath))
        End Sub

        <Fact>
        Public Sub GetAnalyzerReferencesInExtensions()
            Dim extensionManager = New VisualStudioDiagnosticAnalyzerProvider(
                New MockExtensionManager({({"installPath1", "installPath2", "installPath3"}, "Vsix")}),
                GetType(MockExtensionManager.MockContent))

            Dim references = extensionManager.GetAnalyzerReferencesInExtensions()

            AssertEx.SetEqual(
            {
                Path.Combine(TempRoot.Root, "InstallPath\installPath1"),
                Path.Combine(TempRoot.Root, "InstallPath\installPath2"),
                Path.Combine(TempRoot.Root, "InstallPath\installPath3")
            },
            references.Select(Function(referenceAndId) referenceAndId.reference.FullPath))
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6285")>
        Public Sub TestHostAnalyzerAssemblyLoader()
            Using tempRoot = New TempRoot
                Dim dir = tempRoot.CreateDirectory
                Dim analyzerFile = DesktopTestHelpers.CreateCSharpAnalyzerAssemblyWithTestAnalyzer(dir, "TestAnalyzer")
                Dim analyzerLoader = VisualStudioDiagnosticAnalyzerProvider.AnalyzerAssemblyLoader
                Dim hostAnalyzers = New HostDiagnosticAnalyzers(ImmutableArray.Create(Of AnalyzerReference)(New AnalyzerFileReference(analyzerFile.Path, analyzerLoader)))
                Dim analyzerReferenceMap = hostAnalyzers.GetOrCreateHostDiagnosticAnalyzersPerReference(LanguageNames.CSharp)
                Assert.Single(analyzerReferenceMap)
                Dim analyzers = analyzerReferenceMap.Single().Value
                Assert.Single(analyzers)
                Assert.Equal("TestAnalyzer", analyzers(0).ToString)
            End Using
        End Sub
    End Class
End Namespace
