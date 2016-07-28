' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    Public Class VisualStudioAnalyzerTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub GetReferenceCalledMultipleTimes()
            Using analyzer = New VisualStudioAnalyzer("C:\Foo\Bar.dll", New MockVsFileChangeEx(), Nothing, Nothing, Nothing, Nothing, Nothing)
                Dim reference1 = analyzer.GetReference()
                Dim reference2 = analyzer.GetReference()

                Assert.True(Object.ReferenceEquals(reference1, reference2))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub AnalyzerErrorsAreUpdated()
            Dim hostDiagnosticUpdateSource = New HostDiagnosticUpdateSource(Nothing, New MockDiagnosticUpdateSourceRegistrationService())

            Dim file = Path.GetTempFileName()
            Dim eventHandler = New EventHandlers(file)

            AddHandler hostDiagnosticUpdateSource.DiagnosticsUpdated, AddressOf eventHandler.DiagnosticAddedTest

            Using analyzer = New VisualStudioAnalyzer(file, New MockVsFileChangeEx(), hostDiagnosticUpdateSource, ProjectId.CreateNewId(), Nothing, New MockAnalyzerAssemblyLoader(), LanguageNames.VisualBasic)
                Dim reference = analyzer.GetReference()
                reference.GetAnalyzers(LanguageNames.VisualBasic)

                RemoveHandler hostDiagnosticUpdateSource.DiagnosticsUpdated, AddressOf eventHandler.DiagnosticAddedTest
                AddHandler hostDiagnosticUpdateSource.DiagnosticsUpdated, AddressOf eventHandler.DiagnosticRemovedTest
            End Using

            IO.File.Delete(file)
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
