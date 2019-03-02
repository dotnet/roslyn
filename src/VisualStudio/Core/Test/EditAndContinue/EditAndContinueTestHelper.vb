' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.EditAndContinue
    Friend Class EditAndContinueTestHelper

        Public Shared Function CreateTestWorkspace() As TestWorkspace
            ' create workspace
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Goo { }
                                    </Document>
                           </Project>
                       </Workspace>
            Return TestWorkspace.Create(test)
        End Function

        Public Class TestDiagnosticAnalyzerService
            Implements IDiagnosticAnalyzerService, IDiagnosticUpdateSource

            Private ReadOnly _data As ImmutableArray(Of DiagnosticData)

            Public Sub New()
            End Sub

            Public Sub New(data As ImmutableArray(Of DiagnosticData))
                Me._data = data
            End Sub

            Public ReadOnly Property SupportGetDiagnostics As Boolean Implements IDiagnosticUpdateSource.SupportGetDiagnostics
                Get
                    Return True
                End Get
            End Property

            Public Event DiagnosticsUpdated As EventHandler(Of DiagnosticsUpdatedArgs) Implements IDiagnosticUpdateSource.DiagnosticsUpdated
            Public Event DiagnosticsCleared As EventHandler Implements IDiagnosticUpdateSource.DiagnosticsCleared

            Public Function GetDiagnostics(workspace As Microsoft.CodeAnalysis.Workspace, projectId As ProjectId, documentId As DocumentId, id As Object, includeSuppressedDiagnostics As Boolean, cancellationToken As CancellationToken) As ImmutableArray(Of DiagnosticData) Implements IDiagnosticUpdateSource.GetDiagnostics
                Return If(includeSuppressedDiagnostics, _data, _data.WhereAsArray(Function(d) Not d.IsSuppressed))
            End Function

            Public Sub Reanalyze(workspace As Microsoft.CodeAnalysis.Workspace, Optional projectIds As IEnumerable(Of ProjectId) = Nothing, Optional documentIds As IEnumerable(Of DocumentId) = Nothing, Optional highPriority As Boolean = False) Implements IDiagnosticAnalyzerService.Reanalyze
            End Sub

            Public Function GetDiagnosticDescriptors(projectOpt As Project) As ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticDescriptor)) Implements IDiagnosticAnalyzerService.CreateDiagnosticDescriptorsPerReference
                Return ImmutableDictionary(Of String, ImmutableArray(Of DiagnosticDescriptor)).Empty
            End Function

            Public Function GetDiagnosticsForSpanAsync(document As Document, range As TextSpan, Optional diagnosticId As String = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of IEnumerable(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForSpanAsync
                Return Task.FromResult(SpecializedCollections.EmptyEnumerable(Of DiagnosticData))
            End Function

            Public Function TryAppendDiagnosticsForSpanAsync(document As Document, range As TextSpan, diagnostics As List(Of DiagnosticData), Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements IDiagnosticAnalyzerService.TryAppendDiagnosticsForSpanAsync
                Return Task.FromResult(False)
            End Function

            Public Function GetSpecificCachedDiagnosticsAsync(workspace As Microsoft.CodeAnalysis.Workspace, id As Object, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetSpecificCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetCachedDiagnosticsAsync(workspace As Microsoft.CodeAnalysis.Workspace, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetCachedDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetSpecificDiagnosticsAsync(solution As Solution, id As Object, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetSpecificDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticsForIdsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional documentId As DocumentId = Nothing, Optional diagnosticIds As ImmutableHashSet(Of String) = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetProjectDiagnosticsForIdsAsync(solution As Solution, Optional projectId As ProjectId = Nothing, Optional diagnosticIds As ImmutableHashSet(Of String) = Nothing, Optional includeSuppressedDiagnostics As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As Task(Of ImmutableArray(Of DiagnosticData)) Implements IDiagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync
                Return SpecializedTasks.EmptyImmutableArray(Of DiagnosticData)()
            End Function

            Public Function GetDiagnosticDescriptors(analyzer As DiagnosticAnalyzer) As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzerService.GetDiagnosticDescriptors
                Return ImmutableArray(Of DiagnosticDescriptor).Empty
            End Function

            Public Function IsCompilerDiagnostic(language As String, diagnostic As DiagnosticData) As Boolean Implements IDiagnosticAnalyzerService.IsCompilerDiagnostic
                Return False
            End Function

            Public Function GetCompilerDiagnosticAnalyzer(language As String) As DiagnosticAnalyzer Implements IDiagnosticAnalyzerService.GetCompilerDiagnosticAnalyzer
                Return Nothing
            End Function

            Public Function IsCompilerDiagnosticAnalyzer(language As String, analyzer As DiagnosticAnalyzer) As Boolean Implements IDiagnosticAnalyzerService.IsCompilerDiagnosticAnalyzer
                Return False
            End Function

            Public Function ContainsDiagnostics(workspace As Microsoft.CodeAnalysis.Workspace, projectId As ProjectId) As Boolean Implements IDiagnosticAnalyzerService.ContainsDiagnostics
                Throw New NotImplementedException()
            End Function

            Public Function GetHostAnalyzerReferences() As IEnumerable(Of AnalyzerReference) Implements IDiagnosticAnalyzerService.GetHostAnalyzerReferences
                Throw New NotImplementedException()
            End Function

            Public Function GetDiagnosticAnalyzers(project As Project) As ImmutableArray(Of DiagnosticAnalyzer) Implements IDiagnosticAnalyzerService.GetDiagnosticAnalyzers
                Throw New NotImplementedException()
            End Function

            Public Function IsCompilationEndAnalyzer(analyzer As DiagnosticAnalyzer, project As Project, compilation As Compilation) As Boolean Implements IDiagnosticAnalyzerService.IsCompilationEndAnalyzer
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
