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

            VisualBasicEditAndContinueTestHelpers.CreateInstance().VerifyUnchangedDocument(
                ActiveStatementsDescription.ClearTags(source),
                description.OldStatements,
                description.NewSpans,
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
            VisualBasicEditAndContinueTestHelpers.CreateInstance().VerifyRudeDiagnostics(editScript, description, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifyLineEdits(editScript As EditScript(Of SyntaxNode),
                                   expectedLineEdits As IEnumerable(Of SourceLineUpdate),
                                   expectedNodeUpdates As IEnumerable(Of String),
                                   ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VisualBasicEditAndContinueTestHelpers.CreateInstance().VerifyLineEdits(editScript, expectedLineEdits, expectedNodeUpdates, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemanticDiagnostics(editScript As EditScript(Of SyntaxNode),
                                             ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifySemantics({editScript}, ActiveStatementsDescription.Empty, Nothing, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemantics(editScript As EditScript(Of SyntaxNode),
                                   activeStatements As ActiveStatementsDescription,
                                   expectedSemanticEdits As SemanticEditDescription(),
                                   ParamArray expectedDiagnostics As RudeEditDiagnosticDescription())
            VerifySemantics({editScript}, activeStatements, expectedSemanticEdits, expectedDiagnostics)
        End Sub

        <Extension>
        Friend Sub VerifySemantics(editScripts As EditScript(Of SyntaxNode)(),
                                   Optional activeStatements As ActiveStatementsDescription = Nothing,
                                   Optional expectedSemanticEdits As SemanticEditDescription() = Nothing,
                                   Optional expectedDiagnostics As RudeEditDiagnosticDescription() = Nothing)
            VisualBasicEditAndContinueTestHelpers.CreateInstance().VerifySemantics(
                editScripts,
                activeStatements,
                expectedSemanticEdits,
                expectedDiagnostics)
        End Sub
    End Module
End Namespace
