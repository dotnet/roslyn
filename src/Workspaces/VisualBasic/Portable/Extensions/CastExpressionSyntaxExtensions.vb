' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
