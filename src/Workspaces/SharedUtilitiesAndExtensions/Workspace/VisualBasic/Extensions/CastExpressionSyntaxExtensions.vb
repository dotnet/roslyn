' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module CastExpressionSyntaxExtensions
        <Extension>
        Public Function Uncast(cast As CastExpressionSyntax) As ExpressionSyntax
            Return Uncast(cast, cast.Expression)
        End Function

        <Extension>
        Public Function Uncast(cast As PredefinedCastExpressionSyntax) As ExpressionSyntax
            Return Uncast(cast, cast.Expression)
        End Function

        Private Function Uncast(castNode As ExpressionSyntax, innerNode As ExpressionSyntax) As ExpressionSyntax
            Dim resultNode = innerNode.WithTriviaFrom(castNode)

            resultNode = SimplificationHelpers.CopyAnnotations(castNode, resultNode)

            Return resultNode

        End Function
    End Module
End Namespace
