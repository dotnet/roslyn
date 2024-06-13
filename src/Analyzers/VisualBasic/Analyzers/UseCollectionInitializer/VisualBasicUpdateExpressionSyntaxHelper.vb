' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    Friend NotInheritable Class VisualBasicUpdateExpressionSyntaxHelper
        Implements IUpdateExpressionSyntaxHelper(Of ExpressionSyntax, StatementSyntax)

        Public Shared ReadOnly Instance As New VisualBasicUpdateExpressionSyntaxHelper()

        Public Sub GetPartsOfForeachStatement(statement As StatementSyntax, ByRef identifier As SyntaxToken, ByRef expression As ExpressionSyntax, ByRef statements As IEnumerable(Of StatementSyntax)) Implements IUpdateExpressionSyntaxHelper(Of ExpressionSyntax, StatementSyntax).GetPartsOfForeachStatement
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Sub

        Public Sub GetPartsOfIfStatement(statement As StatementSyntax, ByRef condition As ExpressionSyntax, ByRef whenTrueStatements As IEnumerable(Of StatementSyntax), ByRef whenFalseStatements As IEnumerable(Of StatementSyntax)) Implements IUpdateExpressionSyntaxHelper(Of ExpressionSyntax, StatementSyntax).GetPartsOfIfStatement
            ' Only called for collection expressions, which VB does not support
            Throw ExceptionUtilities.Unreachable()
        End Sub
    End Class
End Namespace
