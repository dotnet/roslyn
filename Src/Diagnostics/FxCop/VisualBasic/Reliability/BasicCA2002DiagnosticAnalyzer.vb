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
        Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

        Private Shared ReadOnly _kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(Of SyntaxKind)(SyntaxKind.SyncLockStatement)
        Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
            Get
                Return _kindsOfInterest
            End Get
        End Property

        Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
            Dim lockStatement = DirectCast(node, SyncLockStatementSyntax)
            MyBase.GetDiagnosticsForNode(lockStatement.Expression, semanticModel, addDiagnostic)
        End Sub
    End Class
End Namespace
