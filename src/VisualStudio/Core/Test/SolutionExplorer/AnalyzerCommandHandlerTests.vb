﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
            Dim diagnosticItem = New DiagnosticItem(Nothing, Nothing, descriptor, ReportDiagnostic.Error, LanguageNames.VisualBasic, Nothing)

            Dim handler = New AnalyzersCommandHandler(Nothing, Nothing, Nothing)
            Dim shown = handler.DiagnosticContextMenuController.ShowContextMenu({diagnosticItem}, Nothing)
            Assert.False(shown)
        End Sub
    End Class
End Namespace
