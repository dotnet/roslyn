' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module ObjectCreationExpressionExtensions

        <Extension>
        Public Function CanRemoveEmptyArgumentList(objectCreationExpression As ObjectCreationExpressionSyntax, semanticModel As SemanticModel) As Boolean
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
