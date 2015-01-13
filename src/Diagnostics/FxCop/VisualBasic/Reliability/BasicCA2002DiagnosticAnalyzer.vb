' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Reliability
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Usage

    ''' <summary>
    ''' CA2002: Do not lock on objects with weak identities
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA2002DiagnosticAnalyzer
        Inherits CA2002DiagnosticAnalyzer

        Public Overrides Sub Initialize(analysisContext As AnalysisContext)
            AnalysisContext.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.SyncLockStatement)
        End Sub

        Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim lockStatement = DirectCast(context.Node, SyncLockStatementSyntax)
            MyBase.GetDiagnosticsForNode(lockStatement.Expression, context.SemanticModel, AddressOf context.ReportDiagnostic)
        End Sub
    End Class
End Namespace
