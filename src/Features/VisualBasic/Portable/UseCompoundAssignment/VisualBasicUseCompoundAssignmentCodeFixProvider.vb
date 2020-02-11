﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.UseCompoundAssignment
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment

    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicUseCompoundAssignmentCodeFixProvider
        Inherits AbstractUseCompoundAssignmentCodeFixProvider(Of SyntaxKind, AssignmentStatementSyntax, ExpressionSyntax)

        <ImportingConstructor>
        Public Sub New()
            MyBase.New(Kinds)
        End Sub

        Protected Overrides Function GetSyntaxKind(rawKind As Integer) As SyntaxKind
            Return CType(rawKind, SyntaxKind)
        End Function

        Protected Overrides Function Token(kind As SyntaxKind) As SyntaxToken
            Return SyntaxFactory.Token(kind)
        End Function

        Protected Overrides Function Assignment(
            assignmentOpKind As SyntaxKind, left As ExpressionSyntax, syntaxToken As SyntaxToken, right As ExpressionSyntax) As AssignmentStatementSyntax

            Return SyntaxFactory.AssignmentStatement(assignmentOpKind, left, syntaxToken, right)
        End Function
    End Class
End Namespace
