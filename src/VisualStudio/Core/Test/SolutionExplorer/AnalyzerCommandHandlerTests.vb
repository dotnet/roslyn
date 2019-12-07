' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class AnalyzerCommandHandlerTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        <WorkItem(36304, "https://github.com/dotnet/roslyn/issues/36304")>
        Public Sub TestLazyInitialization()
            Dim descriptor = New DiagnosticDescriptor(
                id:="TST0001",
                title:="A test diagnostic",
                messageFormat:="A test message",
                category:="Test",
                defaultSeverity:=DiagnosticSeverity.Error,
                isEnabledByDefault:=True)
            Dim diagnosticItem = New LegacyDiagnosticItem(Nothing, descriptor, ReportDiagnostic.Error, LanguageNames.VisualBasic, Nothing)

            Dim handler = New AnalyzersCommandHandler(Nothing, Nothing, Nothing)
            Dim shown = handler.DiagnosticContextMenuController.ShowContextMenu({diagnosticItem}, Nothing)
            Debug.Assert(Not shown)
        End Sub
    End Class
End Namespace
