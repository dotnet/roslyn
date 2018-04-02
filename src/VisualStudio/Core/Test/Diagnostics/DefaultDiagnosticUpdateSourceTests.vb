﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    <[UseExportProvider]>
    Public Class DefaultDiagnosticUpdateSourceTests
        <WpfFact>
        Public Async Function TestMiscSquiggles() As Task
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = TestWorkspace.CreateCSharp(code.Value)
                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Dim diagnosticService = New DiagnosticService(listenerProvider)

                Dim miscService = New DefaultDiagnosticAnalyzerService(diagnosticService)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Syntax)

                Dim buffer = workspace.Documents.First().GetTextBuffer()

                WpfTestCase.RequireWpfFact("This test uses IForegroundNotificationService")
                Dim foregroundService = workspace.GetService(Of IForegroundNotificationService)()
                Dim provider = New DiagnosticsSquiggleTaggerProvider(diagnosticService, foregroundService, listenerProvider)
                Dim tagger = provider.CreateTagger(Of IErrorTag)(buffer)
                Using disposable = TryCast(tagger, IDisposable)
                    Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                    Await analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), InvocationReasons.Empty, CancellationToken.None)

                    Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).CreateWaitTask()
                    Await listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).CreateWaitTask()

                    Dim snapshot = buffer.CurrentSnapshot
                    Dim spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToImmutableArray()

                    Assert.True(spans.Count() > 0)
                    Assert.True(spans.All(Function(s) s.Span.Length > 0))
                End Using
            End Using
        End Function

        <Fact>
        Public Async Function TestDefaultDiagnosticProviderSyntax() As Task
            Dim code = <code>
class A
{
   void Method()
   {
        M m = null
   }
}
                       </code>

            Using workspace = TestWorkspace.CreateCSharp(code.Value)
                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Dim diagnosticService = New DiagnosticService(listenerProvider)

                Dim miscService = New DefaultDiagnosticAnalyzerService(diagnosticService)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Syntax)

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)

                Await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.AnalyzeDocumentAsync(document, Nothing, InvocationReasons.Empty, CancellationToken.None)

                Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).CreateWaitTask()
                Await listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).CreateWaitTask()

                Assert.True(
                    diagnosticService.GetDiagnostics(workspace, document.Project.Id, document.Id, Nothing, False, CancellationToken.None).Count() = 1)
            End Using
        End Function

        <Fact>
        Public Async Function TestDefaultDiagnosticProviderSemantic() As Task
            Dim code = <code>
class A
{
   void Method()
   {
        M m = null
   }
}
                       </code>

            Using workspace = TestWorkspace.CreateCSharp(code.Value)
                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Dim diagnosticService = New DiagnosticService(listenerProvider)

                Dim miscService = New DefaultDiagnosticAnalyzerService(diagnosticService)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Semantic)

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.AnalyzeDocumentAsync(document, Nothing, InvocationReasons.Empty, CancellationToken.None)

                Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).CreateWaitTask()
                Await listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).CreateWaitTask()

                Assert.True(
                    diagnosticService.GetDiagnostics(workspace, document.Project.Id, document.Id, Nothing, False, CancellationToken.None).Count() = 1)
            End Using
        End Function

        <Fact>
        Public Async Function TestDefaultDiagnosticProviderAll() As Task
            Dim code = <code>
class A
{
   void Method()
   {
        M m = null
   }
}
                       </code>

            Using workspace = TestWorkspace.CreateCSharp(code.Value)
                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Dim diagnosticService = New DiagnosticService(listenerProvider)

                Dim miscService = New DefaultDiagnosticAnalyzerService(diagnosticService)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Semantic Or DiagnosticProvider.Options.Syntax)

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.AnalyzeDocumentAsync(document, Nothing, InvocationReasons.Empty, CancellationToken.None)

                Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).CreateWaitTask()
                Await listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).CreateWaitTask()

                Assert.True(
                    diagnosticService.GetDiagnostics(workspace, document.Project.Id, document.Id, Nothing, False, CancellationToken.None).Count() = 2)
            End Using
        End Function

        <Fact>
        Public Async Function TestDefaultDiagnosticProviderRemove() As Task
            Dim code = <code>
class A
{
   void Method()
   {
        M m = null
   }
}
                       </code>

            Using workspace = TestWorkspace.CreateCSharp(code.Value)
                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Dim diagnosticService = New DiagnosticService(listenerProvider)

                Dim miscService = New DefaultDiagnosticAnalyzerService(diagnosticService)
                Assert.False(miscService.SupportGetDiagnostics)

                DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Semantic Or DiagnosticProvider.Options.Syntax)

                Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(document, InvocationReasons.Empty, CancellationToken.None)
                Await analyzer.AnalyzeDocumentAsync(document, Nothing, InvocationReasons.Empty, CancellationToken.None)

                analyzer.RemoveDocument(document.Id)

                Await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).CreateWaitTask()
                Await listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).CreateWaitTask()

                Assert.True(
                    diagnosticService.GetDiagnostics(workspace, document.Project.Id, document.Id, Nothing, False, CancellationToken.None).Count() = 0)
            End Using
        End Function

        <Fact>
        Public Async Function TestMiscCSharpErrorSource() As Tasks.Task
            Dim code = <code>
class 123 { }
                       </code>
            Using workspace = TestWorkspace.CreateCSharp(code.Value)
                Dim miscService = New DefaultDiagnosticAnalyzerService(New MockDiagnosticUpdateSourceRegistrationService())

                DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Syntax)

                Dim buildTool = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Id, BuildToolId)
                                                               buildTool = id.BuildTool
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), InvocationReasons.Empty, CancellationToken.None)

                Assert.Equal(PredefinedBuildTools.Live, buildTool)
            End Using
        End Function

        <Fact>
        Public Async Function TestMiscVBErrorSource() As Task
            Dim code = <code>
Class 123
End Class
                       </code>
            Using workspace = TestWorkspace.CreateVisualBasic(code.Value)
                Dim miscService = New DefaultDiagnosticAnalyzerService(New MockDiagnosticUpdateSourceRegistrationService())

                DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Syntax)

                Dim buildTool = String.Empty

                AddHandler miscService.DiagnosticsUpdated, Sub(e, a)
                                                               Dim id = DirectCast(a.Id, BuildToolId)
                                                               buildTool = id.BuildTool
                                                           End Sub

                Dim analyzer = miscService.CreateIncrementalAnalyzer(workspace)
                Await analyzer.AnalyzeSyntaxAsync(workspace.CurrentSolution.Projects.First().Documents.First(), InvocationReasons.Empty, CancellationToken.None)

                Assert.Equal(PredefinedBuildTools.Live, buildTool)
            End Using
        End Function
    End Class
End Namespace
