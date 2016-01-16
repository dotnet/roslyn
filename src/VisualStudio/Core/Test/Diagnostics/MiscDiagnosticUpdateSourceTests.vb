' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Public Class MiscDiagnosticUpdateSourceTests
        <WpfFact>
        Public Async Function TestMiscSquiggles() As Task
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = Await CSharpWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(code.ToString())
                Dim miscService = New MiscellaneousDiagnosticAnalyzerService(
                    New TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()),
                    New MockDiagnosticUpdateSourceRegistrationService())

                Assert.False(miscService.SupportGetDiagnostics)

                Dim listener = New AsynchronousOperationListener()
                Dim listeners = AsynchronousOperationListener.CreateListeners(
                    ValueTuple.Create(FeatureAttribute.DiagnosticService, listener),
                    ValueTuple.Create(FeatureAttribute.ErrorSquiggles, listener))

                Dim diagnosticService = New DiagnosticService(listeners)
                diagnosticService.Register(miscService)

                Dim optionsService = workspace.Services.GetService(Of IOptionService)()

                Dim buffer = workspace.Documents.First().GetTextBuffer()

                WpfTestCase.RequireWpfFact("This test uses IForegroundNotificationService")
                Dim foregroundService = workspace.GetService(Of IForegroundNotificationService)()
                Dim provider = New DiagnosticsSquiggleTaggerProvider(optionsService, diagnosticService, foregroundService, listeners)
                Dim tagger = provider.CreateTagger(Of IErrorTag)(buffer)
                Using disposable = TryCast(tagger, IDisposable)
                    Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                    Await analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), CancellationToken.None)

                    Await listener.CreateWaitTask()

                    Dim snapshot = buffer.CurrentSnapshot
                    Dim spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToImmutableArray()

                    Assert.True(spans.Count() > 0)
                    Assert.True(spans.All(Function(s) s.Span.Length > 0))
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestMiscCSharpErrorSource() As Tasks.Task
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = Await CSharpWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(code.ToString())
                Dim miscService = New MiscellaneousDiagnosticAnalyzerService(
                    New TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()),
                    New MockDiagnosticUpdateSourceRegistrationService())

                Dim buildTool = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Id, BuildToolId)
                                                               buildTool = id.BuildTool
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), CancellationToken.None)

                Assert.Equal(PredefinedBuildTools.Live, buildTool)
            End Using
        End Function

        <Fact>
        Public Async Function TestMiscVBErrorSource() As Task
            Dim code = <code>
Class 123
End Class
                       </code>
            Using workspace = Await VisualBasicWorkspaceFactory.CreateVisualBasicWorkspaceFromFileAsync(code.ToString())
                Dim miscService = New MiscellaneousDiagnosticAnalyzerService(
                    New TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()),
                    New MockDiagnosticUpdateSourceRegistrationService())

                Dim buildTool = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Id, BuildToolId)
                                                               buildTool = id.BuildTool
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), CancellationToken.None)

                Assert.Equal(PredefinedBuildTools.Live, buildTool)
            End Using
        End Function
    End Class
End Namespace