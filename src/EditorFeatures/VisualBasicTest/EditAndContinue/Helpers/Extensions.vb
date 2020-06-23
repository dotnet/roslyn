' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue
Imports Microsoft.VisualStudio.Composition

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Friend Module Extensions

        Private ReadOnly s_exportProviderFactoryWithTestActiveStatementSpanTracker As IExportProviderFactory =
            ExportProviderCache.GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic _
                .WithPart(GetType(TestActiveStatementSpanTracker)))

        Friend Sub VerifyUnchangedDocument(
            source As String,
            description As ActiveStatementsDescription)

            VisualBasicEditAndContinueTestHelpers.Instance.VerifyUnchangedDocument(
                ActiveStatementsDescription.ClearTags(source),
                description.OldStatements,
                description.OldTrackingSpans,
                description.NewSpans,
                description.OldRegions,
                description.NewRegions)
        End Sub

        <Extension>
        Friend Sub VerifyRudeDiagnostics(editScript As EditScript(Of SyntaxNode),
                                         ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifyRudeDiagnostics(editScript, ActiveStatementsDescription.Empty, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifyRudeDiagnostics(editScript As EditScript(Of SyntaxNode),
                                         description As ActiveStatementsDescription,
                                         ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VisualBasicEditAndContinueTestHelpers.Instance.VerifyRudeDiagnostics(editScript, description, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifyLineEdits(editScript As EditScript(Of SyntaxNode),
                                   expectedLineEdits As IEnumerable(Of LineChange),
                                   expectedNodeUpdates As IEnumerable(Of String),
                                   ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VisualBasicEditAndContinueTestHelpers.Instance.VerifyLineEdits(editScript, expectedLineEdits, expectedNodeUpdates, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemanticDiagnostics(editScript As EditScript(Of SyntaxNode),
                                             ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifySemanticDiagnostics(editScript, Nothing, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemanticDiagnostics(editScript As EditScript(Of SyntaxNode),
                                             expectedDeclarationError As DiagnosticDescription,
                                             ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifySemantics(editScript, ActiveStatementsDescription.Empty, Nothing, expectedDeclarationError, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemantics(editScript As EditScript(Of SyntaxNode),
                                   activeStatements As ActiveStatementsDescription,
                                   expectedSemanticEdits As SemanticEditDescription(),
                                   ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifySemantics(editScript, activeStatements, Nothing, Nothing, expectedSemanticEdits, Nothing, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemantics(editScript As EditScript(Of SyntaxNode),
                                   activeStatements As ActiveStatementsDescription,
                                   expectedSemanticEdits As SemanticEditDescription(),
                                   expectedDeclarationError As DiagnosticDescription,
                                   ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifySemantics(editScript, activeStatements, Nothing, Nothing, expectedSemanticEdits, expectedDeclarationError, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemantics(editScript As EditScript(Of SyntaxNode),
                                   activeStatements As ActiveStatementsDescription,
                                   additionalOldSources As IEnumerable(Of String),
                                   additionalNewSources As IEnumerable(Of String),
                                   expectedSemanticEdits As SemanticEditDescription(),
                                   ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifySemantics(editScript, activeStatements, additionalOldSources, additionalNewSources, expectedSemanticEdits, Nothing, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemantics(editScript As EditScript(Of SyntaxNode),
                                   activeStatements As ActiveStatementsDescription,
                                   additionalOldSources As IEnumerable(Of String),
                                   additionalNewSources As IEnumerable(Of String),
                                   expectedSemanticEdits As SemanticEditDescription(),
                                   expectedDeclarationError As DiagnosticDescription,
                                   ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            Using workspace = TestWorkspace.CreateVisualBasic("", exportProvider:=s_exportProviderFactoryWithTestActiveStatementSpanTracker.CreateExportProvider())
                VisualBasicEditAndContinueTestHelpers.Instance.VerifySemantics(
                    workspace,
                    editScript,
                    activeStatements,
                    additionalOldSources,
                    additionalNewSources,
                    expectedSemanticEdits,
                    expectedDeclarationError,
                    expectedDiagnostics)
            End Using
        End Sub
    End Module
End Namespace
