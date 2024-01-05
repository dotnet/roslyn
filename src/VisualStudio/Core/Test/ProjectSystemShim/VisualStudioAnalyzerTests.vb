﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Workspaces.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Diagnostics)>
    Public Class VisualStudioAnalyzerTests
        Private Shared ReadOnly s_compositionWithMockDiagnosticUpdateSourceRegistrationService As TestComposition = EditorTestCompositions.EditorFeatures _
            .AddExcludedPartTypes(GetType(IDiagnosticUpdateSourceRegistrationService)) _
            .AddParts(GetType(MockDiagnosticUpdateSourceRegistrationService))

        <WpfFact>
        Public Sub GetReferenceCalledMultipleTimes()
            Using workspace = New TestWorkspace(composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
                Dim lazyWorkspace = New Lazy(Of VisualStudioWorkspace)(
                                    Function()
                                        Return Nothing
                                    End Function)

                Dim registrationService = Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(workspace.GetService(Of IDiagnosticUpdateSourceRegistrationService)())
                Dim hostDiagnosticUpdateSource = New HostDiagnosticUpdateSource(lazyWorkspace, registrationService)

                Using tempRoot = New TempRoot(), analyzer = New ProjectAnalyzerReference(tempRoot.CreateFile().Path, hostDiagnosticUpdateSource, ProjectId.CreateNewId(), LanguageNames.VisualBasic)
                    Dim reference1 = analyzer.GetReference()
                    Dim reference2 = analyzer.GetReference()

                    Assert.True(Object.ReferenceEquals(reference1, reference2))
                End Using
            End Using
        End Sub

        <WpfFact>
        Public Sub AnalyzerErrorsAreUpdated()
            Using workspace = New TestWorkspace(composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService)
                Dim lazyWorkspace = New Lazy(Of VisualStudioWorkspace)(
                                        Function()
                                            Return Nothing
                                        End Function)

                Dim file = Path.GetTempFileName()

                Dim registrationService = Assert.IsType(Of MockDiagnosticUpdateSourceRegistrationService)(workspace.GetService(Of IDiagnosticUpdateSourceRegistrationService)())
                Dim hostDiagnosticUpdateSource = New HostDiagnosticUpdateSource(lazyWorkspace, registrationService)

                Dim eventHandler = New EventHandlers(file)
                AddHandler hostDiagnosticUpdateSource.DiagnosticsUpdated, AddressOf eventHandler.DiagnosticAddedTest

                Using analyzer = New ProjectAnalyzerReference(file, hostDiagnosticUpdateSource, ProjectId.CreateNewId(), LanguageNames.VisualBasic)
                    Dim reference = analyzer.GetReference()
                    reference.GetAnalyzers(LanguageNames.VisualBasic)

                    RemoveHandler hostDiagnosticUpdateSource.DiagnosticsUpdated, AddressOf eventHandler.DiagnosticAddedTest
                    AddHandler hostDiagnosticUpdateSource.DiagnosticsUpdated, AddressOf EventHandlers.DiagnosticRemovedTest
                End Using

                IO.File.Delete(file)
            End Using
        End Sub

        Private Class EventHandlers
            Public File As String

            Public Sub New(file As String)
                Me.File = file
            End Sub

            Public Sub DiagnosticAddedTest(o As Object, e As ImmutableArray(Of DiagnosticsUpdatedArgs))
                Dim diagnostics = e.Single().Diagnostics
                Assert.Equal(1, diagnostics.Length)
                Dim diagnostic As DiagnosticData = diagnostics.First()
                Assert.Equal("BC42378", diagnostic.Id)
                Assert.Contains(File, diagnostic.Message, StringComparison.Ordinal)
            End Sub

            Public Shared Sub DiagnosticRemovedTest(o As Object, e As ImmutableArray(Of DiagnosticsUpdatedArgs))
                Dim diagnostics = e.Single().Diagnostics
                Assert.Equal(0, diagnostics.Length)
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
