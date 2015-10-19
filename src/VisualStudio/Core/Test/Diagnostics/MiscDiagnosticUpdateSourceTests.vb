﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
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
        <Fact>
        Public Sub TestMiscSquiggles()
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(code.ToString())
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

                Dim foregroundService = workspace.GetService(Of IForegroundNotificationService)()
                Dim provider = New DiagnosticsSquiggleTaggerProvider(optionsService, diagnosticService, foregroundService, listeners)
                Dim tagger = provider.CreateTagger(Of IErrorTag)(buffer)
                Using disposable = TryCast(tagger, IDisposable)
                    Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                    analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), CancellationToken.None).PumpingWait()

                    listener.CreateWaitTask().PumpingWait()

                    Dim snapshot = buffer.CurrentSnapshot
                    Dim spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToImmutableArray()

                    Assert.True(spans.Count() > 0)
                    Assert.True(spans.All(Function(s) s.Span.Length > 0))
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub TestMiscCSharpErrorSource()
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(code.ToString())
                Dim miscService = New MiscellaneousDiagnosticAnalyzerService(
                    New TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()),
                    New MockDiagnosticUpdateSourceRegistrationService())

                Dim buildTool = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Id, BuildToolId)
                                                               buildTool = id.BuildTool
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), CancellationToken.None).PumpingWait()

                Assert.Equal(PredefinedBuildTools.Live, buildTool)
            End Using
        End Sub

        <Fact>
        Public Sub TestMiscVBErrorSource()
            Dim code = <code>
Class 123
End Class
                       </code>
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code.ToString())
                Dim miscService = New MiscellaneousDiagnosticAnalyzerService(
                    New TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()),
                    New MockDiagnosticUpdateSourceRegistrationService())

                Dim buildTool = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Id, BuildToolId)
                                                               buildTool = id.BuildTool
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), CancellationToken.None).PumpingWait()

                Assert.Equal(PredefinedBuildTools.Live, buildTool)
            End Using
        End Sub
    End Class
End Namespace