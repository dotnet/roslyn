' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Class ImportsStatementComparer
        Implements IComparer(Of ImportsStatementSyntax)

        Public Shared ReadOnly SystemFirstInstance As IComparer(Of ImportsStatementSyntax) = New ImportsStatementComparer(TokenComparer.SystemFirstInstance)
        Public Shared ReadOnly NormalInstance As IComparer(Of ImportsStatementSyntax) = New ImportsStatementComparer(TokenComparer.NormalInstance)

        Private ReadOnly _importsClauseComparer As IComparer(Of ImportsClauseSyntax)

        Public Sub New(tokenComparer As IComparer(Of SyntaxToken))
            Debug.Assert(tokenComparer IsNot Nothing)
            Me._importsClauseComparer = New ImportsClauseComparer(tokenComparer)
        End Sub

        Public Function Compare(directive1 As ImportsStatementSyntax, directive2 As ImportsStatementSyntax) As Integer Implements IComparer(Of ImportsStatementSyntax).Compare
            If directive1 Is directive2 Then
                Return 0
            End If

            ' the clauses will already be sorted by now.
            If directive1.ImportsClauses.Count = 0 And directive2.ImportsClauses.Count = 0 Then
                Return 0
            ElseIf directive1.ImportsClauses.Count = 0 Then
                Return -1
            ElseIf directive2.ImportsClauses.Count = 0 Then
                Return 1
            Else
                Return _importsClauseComparer.Compare(directive1.ImportsClauses(0), directive2.ImportsClauses(0))
            End If
        End Function
    End Class
End Namespace
