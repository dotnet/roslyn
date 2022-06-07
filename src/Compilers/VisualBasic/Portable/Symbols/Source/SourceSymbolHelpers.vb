' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module SourceSymbolHelpers

        Public Function GetAsClauseLocation(identifier As SyntaxToken, asClauseOpt As AsClauseSyntax) As SyntaxNodeOrToken
            If asClauseOpt IsNot Nothing AndAlso
                (asClauseOpt.Kind <> SyntaxKind.AsNewClause OrElse
                (DirectCast(asClauseOpt, AsNewClauseSyntax).NewExpression.Kind <> SyntaxKind.AnonymousObjectCreationExpression)) Then
                Return asClauseOpt.Type
            Else
                Return identifier
            End If
        End Function

    End Module

End Namespace
