' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Analyzer.Utilities.Extensions
Imports Microsoft.CodeAnalysis.Analyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class VisualBasicUpgradeMSBuildWorkspaceAnalyzer
        Inherits UpgradeMSBuildWorkspaceAnalyzer

        Private Protected Sub New(performAssemblyChecks As Boolean)
            MyBase.New(performAssemblyChecks)
        End Sub

        Public Sub New()
            Me.New(performAssemblyChecks:=True)
        End Sub

        Protected Overrides Sub RegisterIdentifierAnalysis(context As CompilationStartAnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeIdentifier, SyntaxKind.IdentifierName)
        End Sub

        Protected Overrides Sub RegisterIdentifierAnalysis(context As AnalysisContext)
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