' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class MiscDiagnosticUpdateSourceTests
        <Fact>
        Public Sub TestMiscSquiggles()
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(code.ToString())
                Dim miscService = New MiscellaneousDiagnosticAnalyzerService(New TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()))
                Assert.False(miscService.SupportGetDiagnostics)

                Dim diagnosticWaiter = New DiagnosticServiceWaiter()
                Dim listener = SpecializedCollections.SingletonEnumerable(New Lazy(Of IAsynchronousOperationListener, FeatureMetadata)(
                    Function() diagnosticWaiter,
                    New FeatureMetadata(New Dictionary(Of String, Object)() From {{"FeatureName", FeatureAttribute.DiagnosticService}})))

                Dim diagnosticService = New DiagnosticService(New IDiagnosticUpdateSource() {miscService}, listener)

                Dim optionsService = workspace.Services.GetService(Of IOptionService)()

                Dim buffer = workspace.Documents.First().GetTextBuffer()

                Dim squiggleWaiter = New ErrorSquiggleWaiter()
                Dim foregroundService = New TestForegroundNotificationService()
                Dim taggerSource = New DiagnosticsSquiggleTaggerProvider.TagSource(buffer, foregroundService, diagnosticService, optionsService, squiggleWaiter)

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), CancellationToken.None).PumpingWait()

                diagnosticWaiter.CreateWaitTask().PumpingWait()
                squiggleWaiter.CreateWaitTask().PumpingWait()

                Dim snapshot = buffer.CurrentSnapshot
                Dim intervalTree = taggerSource.GetTagIntervalTreeForBuffer(buffer)
                Dim spans = intervalTree.GetIntersectingSpans(New SnapshotSpan(snapshot, 0, snapshot.Length)).ToImmutableArray()

                Assert.True(spans.Count() > 0)
                Assert.True(spans.All(Function(s) s.Span.Length > 0))

                taggerSource.TestOnly_Dispose()
            End Using
        End Sub

        <Fact>
        Public Sub TestMiscCSharpErrorSource()
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(code.ToString())
                Dim miscService = New MiscellaneousDiagnosticAnalyzerService(New TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()))
                Dim errorSource = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Id, ErrorSourceId)
                                                               errorSource = id.ErrorSource
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), CancellationToken.None).PumpingWait()

                Assert.Equal(PredefinedErrorSources.Compiler, errorSource)
            End Using
        End Sub

        <Fact>
        Public Sub TestMiscVBErrorSource()
            Dim code = <code>
Class 123
End Class
                       </code>
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code.ToString())
                Dim miscService = New MiscellaneousDiagnosticAnalyzerService(New TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()))
                Dim errorSource = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Id, ErrorSourceId)
                                                               errorSource = id.ErrorSource
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), CancellationToken.None).PumpingWait()

                Assert.Equal(PredefinedErrorSources.Compiler, errorSource)
            End Using
        End Sub

        Private Class DiagnosticServiceWaiter : Inherits AsynchronousOperationListener : End Class
        Private Class ErrorSquiggleWaiter : Inherits AsynchronousOperationListener : End Class
    End Class
End Namespace