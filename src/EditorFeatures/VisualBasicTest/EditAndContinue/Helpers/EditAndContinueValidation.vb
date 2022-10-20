' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.EditAndContinue.Contracts
Imports Microsoft.CodeAnalysis.EditAndContinue.UnitTests
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EditAndContinue

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue.UnitTests

    Friend Module EditAndContinueValidation
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
                                   lineEdits As SourceLineUpdate(),
                                   Optional semanticEdits As SemanticEditDescription() = Nothing,
                                   Optional diagnostics As RudeEditDiagnosticDescription() = Nothing)
            Assert.NotEmpty(lineEdits)

            VerifyLineEdits(
                editScript,
                {New SequencePointUpdates(editScript.Match.OldRoot.SyntaxTree.FilePath, lineEdits.ToImmutableArray())},
                semanticEdits,
                diagnostics)
        End Sub

        <Extension>
        Friend Sub VerifyLineEdits(editScript As EditScript(Of SyntaxNode),
                                   lineEdits As SequencePointUpdates(),
                                   Optional semanticEdits As SemanticEditDescription() = Nothing,
                                   Optional diagnostics As RudeEditDiagnosticDescription() = Nothing)
            Dim validator = New VisualBasicEditAndContinueTestHelpers()
            validator.VerifyLineEdits(editScript, lineEdits, semanticEdits, diagnostics)
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
                                   Optional targetFrameworks As TargetFramework() = Nothing,
                                   Optional capabilities As EditAndContinueCapabilities? = Nothing)
            VerifySemantics(
                {editScript},
                {New DocumentAnalysisResultsDescription(activeStatements, semanticEdits, lineEdits:=Nothing, diagnostics)},
                targetFrameworks,
                capabilities)
        End Sub

        <Extension>
        Friend Sub VerifySemantics(editScripts As EditScript(Of SyntaxNode)(),
                                   expected As DocumentAnalysisResultsDescription(),
                                   Optional targetFrameworks As TargetFramework() = Nothing,
                                   Optional capabilities As EditAndContinueCapabilities? = Nothing)
            For Each framework In If(targetFrameworks, {TargetFramework.NetStandard20, TargetFramework.NetCoreApp})
                Dim validator = New VisualBasicEditAndContinueTestHelpers()
                validator.VerifySemantics(editScripts, framework, expected, capabilities)
            Next
        End Sub
    End Module
End Namespace
