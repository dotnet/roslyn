' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue
Imports Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Friend Module EditAndContinueValidation

        Friend Sub VerifyUnchangedDocument(
            source As String,
            description As ActiveStatementsDescription)
#If TODO Then
            Dim validator = New VisualBasicEditAndContinueTestHelpers()
            validator.VerifyUnchangedDocument(
                ActiveStatementsDescription.ClearTags(source),
                description.OldStatements,
                description.NewSpans,
                description.NewRegions)
#End If
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
            VerifySemantics(editScript, description, diagnostics:=expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifyLineEdits(editScript As EditScript(Of SyntaxNode),
                                   expectedLineEdits As IEnumerable(Of SourceLineUpdate),
                                   expectedNodeUpdates As IEnumerable(Of String),
                                   ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            Dim validator = New VisualBasicEditAndContinueTestHelpers()
            validator.VerifyLineEdits(editScript, expectedLineEdits, expectedNodeUpdates, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemanticDiagnostics(editScript As EditScript(Of SyntaxNode),
                                             ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifySemantics(
                {editScript},
                {New DocumentAnalysisResultsDescription(diagnostics:=expectedDiagnostics)})
        End Sub

        <Extension>
        Friend Sub VerifySemanticDiagnostics(editScript As EditScript(Of SyntaxNode),
                                             targetFrameworks As TargetFramework(),
                                             ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifySemantics(
                {editScript},
                {New DocumentAnalysisResultsDescription(diagnostics:=expectedDiagnostics)},
                targetFrameworks:=targetFrameworks)
        End Sub

        <Extension>
        Friend Sub VerifySemantics(editScript As EditScript(Of SyntaxNode),
                                   Optional activeStatements As ActiveStatementsDescription = Nothing,
                                   Optional semanticEdits As SemanticEditDescription() = Nothing,
                                   Optional diagnostics As RudeEditDiagnosticDescription() = Nothing,
                                   Optional targetFrameworks As TargetFramework() = Nothing)
            VerifySemantics(
                {editScript},
                {New DocumentAnalysisResultsDescription(activeStatements, semanticEdits, diagnostics)},
                targetFrameworks)
        End Sub

        <Extension>
        Friend Sub VerifySemantics(editScripts As EditScript(Of SyntaxNode)(),
                                   expected As DocumentAnalysisResultsDescription(),
                                   Optional targetFrameworks As TargetFramework() = Nothing)
            For Each framework In If(targetFrameworks, {TargetFramework.NetStandard20, TargetFramework.NetCoreApp})
                Dim validator = New VisualBasicEditAndContinueTestHelpers()
                validator.VerifySemantics(editScripts, framework, expected)
            Next
        End Sub
    End Module
End Namespace
