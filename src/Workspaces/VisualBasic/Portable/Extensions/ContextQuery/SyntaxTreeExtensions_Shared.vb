' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
    Partial Friend Module SyntaxTreeExtensions
        ' Tuple literals aren't recognized by the parser until there is a comma
        ' So a parenthesized expression is a possible tuple context too
        <Extension>
        Friend Function IsPossibleTupleContext(syntaxTree As SyntaxTree,
                                               tokenOnLeftOfPosition As SyntaxToken,
                                               position As Integer) As Boolean

            tokenOnLeftOfPosition = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position)

            If tokenOnLeftOfPosition.IsKind(SyntaxKind.OpenParenToken) Then
                Return tokenOnLeftOfPosition.Parent.IsKind(SyntaxKind.ParenthesizedExpression,
                                                           SyntaxKind.TupleExpression, SyntaxKind.TupleType)
            End If

            Return tokenOnLeftOfPosition.IsKind(SyntaxKind.CommaToken) AndAlso
                tokenOnLeftOfPosition.Parent.IsKind(SyntaxKind.TupleExpression, SyntaxKind.TupleType)
        End Function
    End Module
End Namespace
