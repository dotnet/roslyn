' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module ObjectCreationExpressionExtensions

        <Extension>
        Public Function CanRemoveEmptyArgumentList(objectCreationExpression As ObjectCreationExpressionSyntax) As Boolean
            If objectCreationExpression.ArgumentList Is Nothing Then
                Return False
            End If

            If objectCreationExpression.ArgumentList.Arguments.Count > 0 Then
                Return False
            End If

            Dim nextToken = objectCreationExpression.GetLastToken.GetNextToken()

            If nextToken.IsKindOrHasMatchingText(SyntaxKind.OpenParenToken) Then
                Return False
            End If

            If nextToken.IsKindOrHasMatchingText(SyntaxKind.DotToken) Then
                If Not TypeOf objectCreationExpression.Type Is PredefinedTypeSyntax Then
                    Return False
                End If
            End If

            Return True
        End Function

    End Module
End Namespace
