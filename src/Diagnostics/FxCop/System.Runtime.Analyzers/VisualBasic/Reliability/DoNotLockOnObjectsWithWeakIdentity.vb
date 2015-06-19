' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace System.Runtime.Analyzers

    ''' <summary>
    ''' CA2002: Do not lock on objects with weak identities
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicDoNotLockOnObjectsWithWeakIdentity
        Inherits DoNotLockOnObjectsWithWeakIdentity

        Public Overrides Sub Initialize(analysisContext As AnalysisContext)
            analysisContext.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.SyncLockStatement)
        End Sub

        Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim lockStatement = DirectCast(context.Node, SyncLockStatementSyntax)
            MyBase.GetDiagnosticsForNode(lockStatement.Expression, context.SemanticModel, AddressOf context.ReportDiagnostic)
        End Sub
    End Class
End Namespace
