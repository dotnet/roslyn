' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class VisualStudioWorkspaceDiagnosticAnalyzerProviderServiceTests
        <Fact>
        Public Sub GetHostAnalyzerPackagesWithNameTest()
            Dim extensionManager = New MockExtensionManager("Microsoft.VisualStudio.Analyzer", "$RootFolder$\test\test.dll", "$ShellFolder$\test\test.dll", "test\test.dll")
            Dim packages = VisualStudioWorkspaceDiagnosticAnalyzerProviderService.GetHostAnalyzerPackagesWithName(extensionManager)

            Assert.Equal(packages.Count(), 3)

            Assert.Equal(packages(0).Name, "Vsix")
            Assert.Equal(packages(0).Assemblies.Length, 1)
            Assert.Equal(packages(0).Assemblies(0), "ResolvedRootFolder\test\test.dll")

            Assert.Equal(packages(1).Name, "Vsix")
            Assert.Equal(packages(1).Assemblies.Length, 1)
            Assert.Equal(packages(1).Assemblies(0), "ResolvedShellFolder\test\test.dll")

            Assert.Equal(packages(2).Name, "Vsix")
            Assert.Equal(packages(2).Assemblies.Length, 1)
            Assert.Equal(packages(2).Assemblies(0), "\InstallPath\test\test.dll")
        End Sub

        <Fact>
        Public Sub GetHostAnalyzerPackagesTest()
            Dim extensionManager = New MockExtensionManager("Microsoft.VisualStudio.Analyzer", "installPath1", "installPath2", "installPath3")
            Dim packages = VisualStudioWorkspaceDiagnosticAnalyzerProviderService.GetHostAnalyzerPackages(extensionManager)

            Assert.Equal(packages.Count(), 1)

            Assert.Null(packages(0).Name)
            Assert.Equal(packages(0).Assemblies.Length, 3)
            Assert.Equal(packages(0).Assemblies(0), "installPath1")
            Assert.Equal(packages(0).Assemblies(1), "installPath2")
            Assert.Equal(packages(0).Assemblies(2), "installPath3")
        End Sub

        <Fact, WorkItem(6285, "https://github.com/dotnet/roslyn/issues/6285")>
        Public Sub TestHostAnalyzerAssemblyLoader()
            Using tempRoot = New TempRoot
                Dim dir = tempRoot.CreateDirectory
                Dim analyzerFile = TestHelpers.CreateCSharpAnalyzerAssemblyWithTestAnalyzer(dir, "TestAnalyzer")
                Dim analyzerPackage = New HostDiagnosticAnalyzerPackage("MyPackage", ImmutableArray.Create(analyzerFile.Path))
                Dim analyzerPackages = SpecializedCollections.SingletonEnumerable(analyzerPackage)
                Dim analyzerLoader = VisualStudioWorkspaceDiagnosticAnalyzerProviderService.GetLoader()
                Dim hostAnalyzerManager = New HostAnalyzerManager(analyzerPackages, analyzerLoader, hostDiagnosticUpdateSource:=Nothing)
                Dim analyzerReferenceMap = hostAnalyzerManager.GetHostDiagnosticAnalyzersPerReference(LanguageNames.CSharp)
                Assert.Single(analyzerReferenceMap)
                Dim analyzers = analyzerReferenceMap.Single().Value
                Assert.Single(analyzers)
                Assert.Equal("TestAnalyzer", analyzers(0).ToString)
            End Using
        End Sub
    End Class
End Namespace
