' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class AnalyzerCommandHandlerTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/36304")>
        Public Sub TestLazyInitialization()
            Dim descriptor = New DiagnosticDescriptor(
                id:="TST0001",
                title:="A test diagnostic",
                messageFormat:="A test message",
                category:="Test",
                defaultSeverity:=DiagnosticSeverity.Error,
                isEnabledByDefault:=True)
            Dim diagnosticItem = New DiagnosticItem(projectId:=Nothing, analyzerReference:=Nothing, descriptor, ReportDiagnostic.Error, commandHandler:=Nothing)

            Dim handler = New AnalyzersCommandHandler(tracker:=Nothing, analyzerReferenceManager:=Nothing, threadingContext:=Nothing, AsynchronousOperationListenerProvider.NullProvider, serviceProvider:=Nothing)
            Dim shown = handler.DiagnosticContextMenuController.ShowContextMenu({diagnosticItem}, location:=Nothing)
            Assert.False(shown)
        End Sub
    End Class
End Namespace
