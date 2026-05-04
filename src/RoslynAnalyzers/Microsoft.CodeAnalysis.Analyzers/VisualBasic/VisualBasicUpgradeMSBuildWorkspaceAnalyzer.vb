' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Analyzer.Utilities.Extensions
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class VisualBasicUpgradeMSBuildWorkspaceAnalyzer
        Inherits UpgradeMSBuildWorkspaceAnalyzer

        Protected Overrides Sub RegisterIdentifierAnalysis(context As CompilationStartAnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeIdentifier, SyntaxKind.IdentifierName)
        End Sub

        Private Sub AnalyzeIdentifier(context As SyntaxNodeAnalysisContext)
            Dim identifierName = TryCast(context.Node, IdentifierNameSyntax)
            If identifierName Is Nothing Then
                Return
            End If

            If Not CaseInsensitiveComparison.Equals(identifierName.Identifier.ToString(), MSBuildWorkspace) Then
                Return
            End If

            Dim symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName, context.CancellationToken)
            If symbolInfo.Symbol Is Nothing Then
                context.ReportDiagnostic(identifierName.CreateDiagnostic(UpgradeMSBuildWorkspaceDiagnosticRule))
            End If
        End Sub
    End Class
End Namespace
