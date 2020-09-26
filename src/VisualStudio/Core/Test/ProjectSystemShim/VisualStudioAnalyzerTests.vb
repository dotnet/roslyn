' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <UseExportProvider>
    Public Class VisualStudioAnalyzerTests
        Private Shared ReadOnly s_compositionWithMockDiagnosticUpdateSourceRegistrationService As TestComposition = EditorTestCompositions.EditorFeatures _
            .AddExcludedPartTypes(GetType(IDiagnosticUpdateSourceRegistrationService)) _
            .AddParts(GetType(MockDiagnosticUpdateSourceRegistrationService))

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub GetReferenceCalledMultipleTimes()
            Dim composition = s_compositionWithMockDiagnosticUpdateSourceRegistrationService
            Dim exportProvider = composition.ExportProviderFactory.CreateExportProvider()

            Using workspace = New TestWorkspace(composition:=composition)
                Dim lazyWorkspace = New Lazy(Of VisualStudioWorkspaceImpl)(
                                    Function()
                                        Return Nothing
                                    End Function)

                Dim registrationService = Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(exportProvider.GetExportedValue(Of IDiagnosticUpdateSourceRegistrationService)())
                Dim hostDiagnosticUpdateSource = New HostDiagnosticUpdateSource(lazyWorkspace, registrationService)

                Using analyzer = New VisualStudioAnalyzer("C:\Goo\Bar.dll", hostDiagnosticUpdateSource, ProjectId.CreateNewId(), LanguageNames.VisualBasic)
                    Dim reference1 = analyzer.GetReference()
                    Dim reference2 = analyzer.GetReference()

                    Assert.True(Object.ReferenceEquals(reference1, reference2))
                End Using
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub AnalyzerErrorsAreUpdated()
            Dim composition = s_compositionWithMockDiagnosticUpdateSourceRegistrationService
            Dim exportProvider = composition.ExportProviderFactory.CreateExportProvider()

            Dim lazyWorkspace = New Lazy(Of VisualStudioWorkspaceImpl)(
                                    Function()
                                        Return Nothing
                                    End Function)

            Dim registrationService = Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(exportProvider.GetExportedValue(Of IDiagnosticUpdateSourceRegistrationService)())
            Dim hostDiagnosticUpdateSource = New HostDiagnosticUpdateSource(lazyWorkspace, registrationService)

            Dim file = Path.GetTempFileName()
            Dim eventHandler = New EventHandlers(file)

            AddHandler hostDiagnosticUpdateSource.DiagnosticsUpdated, AddressOf eventHandler.DiagnosticAddedTest

            Using workspace = New TestWorkspace(composition:=composition)
                Using analyzer = New VisualStudioAnalyzer(file, hostDiagnosticUpdateSource, ProjectId.CreateNewId(), LanguageNames.VisualBasic)
                    Dim reference = analyzer.GetReference()
                    reference.GetAnalyzers(LanguageNames.VisualBasic)

                    RemoveHandler hostDiagnosticUpdateSource.DiagnosticsUpdated, AddressOf eventHandler.DiagnosticAddedTest
                    AddHandler hostDiagnosticUpdateSource.DiagnosticsUpdated, AddressOf eventHandler.DiagnosticRemovedTest
                End Using

                IO.File.Delete(file)
            End Using
        End Sub

        Private Class EventHandlers
            Public File As String

            Public Sub New(file As String)
                Me.File = file
            End Sub

            Public Sub DiagnosticAddedTest(o As Object, e As DiagnosticsUpdatedArgs)
                Assert.Equal(1, e.Diagnostics.Length)
                Dim diagnostic As DiagnosticData = e.Diagnostics.First()
                Assert.Equal("BC42378", diagnostic.Id)
                Assert.Contains(File, diagnostic.Message, StringComparison.Ordinal)
            End Sub

            Public Sub DiagnosticRemovedTest(o As Object, e As DiagnosticsUpdatedArgs)
                Assert.Equal(0, e.Diagnostics.Length)
            End Sub
        End Class

        Private Class MockAnalyzerAssemblyLoader
            Implements IAnalyzerAssemblyLoader

            Public Sub AddDependencyLocation(fullPath As String) Implements IAnalyzerAssemblyLoader.AddDependencyLocation
                Throw New NotImplementedException()
            End Sub

            Public Function LoadFromPath(fullPath As String) As Assembly Implements IAnalyzerAssemblyLoader.LoadFromPath
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
